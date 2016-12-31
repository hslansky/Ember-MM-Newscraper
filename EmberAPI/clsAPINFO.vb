﻿' ################################################################################
' #                             EMBER MEDIA MANAGER                              #
' ################################################################################
' ################################################################################
' # This file is part of Ember Media Manager.                                    #
' #                                                                              #
' # Ember Media Manager is free software: you can redistribute it and/or modify  #
' # it under the terms of the GNU General Public License as published by         #
' # the Free Software Foundation, either version 3 of the License, or            #
' # (at your option) any later version.                                          #
' #                                                                              #
' # Ember Media Manager is distributed in the hope that it will be useful,       #
' # but WITHOUT ANY WARRANTY; without even the implied warranty of               #
' # MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the                #
' # GNU General Public License for more details.                                 #
' #                                                                              #
' # You should have received a copy of the GNU General Public License            #
' # along with Ember Media Manager.  If not, see <http://www.gnu.org/licenses/>. #
' ################################################################################

Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Xml.Serialization
Imports NLog
Imports System.Windows.Forms

Public Class NFO

#Region "Fields"

    Shared logger As Logger = LogManager.GetCurrentClassLogger()

#End Region

#Region "Methods"
    ''' <summary>
    ''' Returns the "merged" result of each data scraper results
    ''' </summary>
    ''' <param name="tDBMovie">Movie to be scraped</param>
    ''' <param name="ScrapedList"><c>List(Of MediaContainers.Movie)</c> which contains unfiltered results of each data scraper</param>
    ''' <returns>The scrape result of movie (after applying various global scraper settings here)</returns>
    ''' <remarks>
    ''' This is used to determine the result of data scraping by going through all scraperesults of every data scraper and applying global data scraper settings here!
    ''' 
    ''' 2014/09/01 Cocotus - First implementation: Moved all global lock settings in various data scrapers to this function, only apply them once and not in every data scraper module! Should be more maintainable!
    ''' </remarks>
    Public Shared Function MergeDataScraperResults_Movie(ByVal tDBMovie As Database.DBElement, ByVal ScrapedList As List(Of MediaContainers.MainDetails)) As Database.DBElement

        'protects the first scraped result against overwriting
        Dim new_Actors As Boolean = False
        Dim new_Certification As Boolean = False
        Dim new_CollectionID As Boolean = False
        Dim new_Collections As Boolean = False
        Dim new_Countries As Boolean = False
        Dim new_Credits As Boolean = False
        Dim new_Directors As Boolean = False
        Dim new_Genres As Boolean = False
        Dim new_MPAA As Boolean = False
        Dim new_OriginalTitle As Boolean = False
        Dim new_Outline As Boolean = False
        Dim new_Plot As Boolean = False
        Dim new_Rating As Boolean = False
        Dim new_ReleaseDate As Boolean = False
        Dim new_Runtime As Boolean = False
        Dim new_Studio As Boolean = False
        Dim new_Tagline As Boolean = False
        Dim new_Title As Boolean = False
        Dim new_Top250 As Boolean = False
        Dim new_Trailer As Boolean = False
        Dim new_UserRating As Boolean = False
        Dim new_Year As Boolean = False

        For Each scrapedmovie In ScrapedList

            'IDs
            If scrapedmovie.IMDBSpecified Then
                tDBMovie.MainDetails.IMDB = scrapedmovie.IMDB
            End If
            If scrapedmovie.TMDBSpecified Then
                tDBMovie.MainDetails.TMDB = scrapedmovie.TMDB
            End If

            'Actors
            If (Not tDBMovie.MainDetails.ActorsSpecified OrElse Not Master.eSettings.MovieLockActors) AndAlso tDBMovie.ScrapeOptions.bMainActors AndAlso
                scrapedmovie.ActorsSpecified AndAlso Master.eSettings.MovieScraperCast AndAlso Not new_Actors Then

                If Master.eSettings.MovieScraperCastWithImgOnly Then
                    For i = scrapedmovie.Actors.Count - 1 To 0 Step -1
                        If String.IsNullOrEmpty(scrapedmovie.Actors(i).URLOriginal) Then
                            scrapedmovie.Actors.RemoveAt(i)
                        End If
                    Next
                End If

                If Master.eSettings.MovieScraperCastLimit > 0 AndAlso scrapedmovie.Actors.Count > Master.eSettings.MovieScraperCastLimit Then
                    scrapedmovie.Actors.RemoveRange(Master.eSettings.MovieScraperCastLimit, scrapedmovie.Actors.Count - Master.eSettings.MovieScraperCastLimit)
                End If

                tDBMovie.MainDetails.Actors = scrapedmovie.Actors
                'added check if there's any actors left to add, if not then try with results of following scraper...
                If scrapedmovie.ActorsSpecified Then
                    new_Actors = True
                    'add numbers for ordering
                    Dim iOrder As Integer = 0
                    For Each actor In scrapedmovie.Actors
                        actor.Order = iOrder
                        iOrder += 1
                    Next
                End If

            ElseIf Master.eSettings.MovieScraperCleanFields AndAlso Not Master.eSettings.MovieScraperCast AndAlso Not Master.eSettings.MovieLockActors Then
                tDBMovie.MainDetails.Actors.Clear()
            End If

            'Certification
            If (Not tDBMovie.MainDetails.CertificationsSpecified OrElse Not Master.eSettings.MovieLockCert) AndAlso tDBMovie.ScrapeOptions.bMainCertifications AndAlso
                scrapedmovie.CertificationsSpecified AndAlso Master.eSettings.MovieScraperCert AndAlso Not new_Certification Then
                If Master.eSettings.MovieScraperCertLang = Master.eLang.All Then
                    tDBMovie.MainDetails.Certifications = scrapedmovie.Certifications
                    new_Certification = True
                Else
                    Dim CertificationLanguage = APIXML.CertLanguagesXML.Language.FirstOrDefault(Function(l) l.abbreviation = Master.eSettings.MovieScraperCertLang)
                    If CertificationLanguage IsNot Nothing AndAlso CertificationLanguage.name IsNot Nothing AndAlso Not String.IsNullOrEmpty(CertificationLanguage.name) Then
                        For Each tCert In scrapedmovie.Certifications
                            If tCert.StartsWith(CertificationLanguage.name) Then
                                tDBMovie.MainDetails.Certifications.Clear()
                                tDBMovie.MainDetails.Certifications.Add(tCert)
                                new_Certification = True
                                Exit For
                            End If
                        Next
                    Else
                        logger.Error("Movie Certification Language (Limit) not found. Please check your settings!")
                    End If
                End If
            ElseIf Master.eSettings.MovieScraperCleanFields AndAlso Not Master.eSettings.MovieScraperCert AndAlso Not Master.eSettings.MovieLockCert Then
                tDBMovie.MainDetails.Certifications.Clear()
            End If

            'Credits
            If (Not tDBMovie.MainDetails.CreditsSpecified OrElse Not Master.eSettings.MovieLockCredits) AndAlso
                scrapedmovie.CreditsSpecified AndAlso Master.eSettings.MovieScraperCredits AndAlso Not new_Credits Then
                tDBMovie.MainDetails.Credits = scrapedmovie.Credits
                new_Credits = True
            ElseIf Master.eSettings.MovieScraperCleanFields AndAlso Not Master.eSettings.MovieScraperCredits AndAlso Not Master.eSettings.MovieLockCredits Then
                tDBMovie.MainDetails.Credits.Clear()
            End If

            'Collection ID
            If (Not tDBMovie.MainDetails.TMDBColIDSpecified OrElse Not Master.eSettings.MovieLockCollectionID) AndAlso tDBMovie.ScrapeOptions.bMainCollectionID AndAlso
                scrapedmovie.TMDBColIDSpecified AndAlso Master.eSettings.MovieScraperCollectionID AndAlso Not new_CollectionID Then
                tDBMovie.MainDetails.TMDBColID = scrapedmovie.TMDBColID
                new_CollectionID = True
            ElseIf Master.eSettings.MovieScraperCleanFields AndAlso Not Master.eSettings.MovieScraperCollectionID AndAlso Not Master.eSettings.MovieLockCollectionID Then
                tDBMovie.MainDetails.TMDBColID = -1
            End If

            'Collections
            If (Not tDBMovie.MainDetails.SetsSpecified OrElse Not Master.eSettings.MovieLockCollections) AndAlso
                scrapedmovie.SetsSpecified AndAlso Master.eSettings.MovieScraperCollectionsAuto AndAlso Not new_Collections Then
                tDBMovie.MainDetails.Sets.Clear()
                For Each movieset In scrapedmovie.Sets
                    If Not String.IsNullOrEmpty(movieset.Title) Then
                        For Each sett As AdvancedSettingsSetting In clsXMLAdvancedSettings.GetAllSettings.Where(Function(y) y.Name.StartsWith("MovieSetTitleRenamer:"))
                            movieset.Title = movieset.Title.Replace(sett.Name.Substring(21), sett.Value)
                        Next
                    End If
                Next
                tDBMovie.MainDetails.Sets.AddRange(scrapedmovie.Sets)
                new_Collections = True
            End If

            'Countries
            If (Not tDBMovie.MainDetails.CountriesSpecified OrElse Not Master.eSettings.MovieLockCountry) AndAlso tDBMovie.ScrapeOptions.bMainCountries AndAlso
                scrapedmovie.CountriesSpecified AndAlso Master.eSettings.MovieScraperCountry AndAlso Not new_Countries Then
                tDBMovie.MainDetails.Countries = scrapedmovie.Countries
                new_Countries = True
            ElseIf Master.eSettings.MovieScraperCleanFields AndAlso Not Master.eSettings.MovieScraperCountry AndAlso Not Master.eSettings.MovieLockCountry Then
                tDBMovie.MainDetails.Countries.Clear()
            End If

            'Directors
            If (Not tDBMovie.MainDetails.DirectorsSpecified OrElse Not Master.eSettings.MovieLockDirector) AndAlso tDBMovie.ScrapeOptions.bMainDirectors AndAlso
                scrapedmovie.DirectorsSpecified AndAlso Master.eSettings.MovieScraperDirector AndAlso Not new_Directors Then
                tDBMovie.MainDetails.Directors = scrapedmovie.Directors
                new_Directors = True
            ElseIf Master.eSettings.MovieScraperCleanFields AndAlso Not Master.eSettings.MovieScraperDirector AndAlso Not Master.eSettings.MovieLockDirector Then
                tDBMovie.MainDetails.Directors.Clear()
            End If

            'Genres
            If (Not tDBMovie.MainDetails.GenresSpecified OrElse Not Master.eSettings.MovieLockGenre) AndAlso tDBMovie.ScrapeOptions.bMainGenres AndAlso
                scrapedmovie.GenresSpecified AndAlso Master.eSettings.MovieScraperGenre AndAlso Not new_Genres Then

                StringUtils.GenreFilter(scrapedmovie.Genres)

                If Master.eSettings.MovieScraperGenreLimit > 0 AndAlso Master.eSettings.MovieScraperGenreLimit < scrapedmovie.Genres.Count AndAlso scrapedmovie.Genres.Count > 0 Then
                    scrapedmovie.Genres.RemoveRange(Master.eSettings.MovieScraperGenreLimit, scrapedmovie.Genres.Count - Master.eSettings.MovieScraperGenreLimit)
                End If
                tDBMovie.MainDetails.Genres = scrapedmovie.Genres
                new_Genres = True
            ElseIf Master.eSettings.MovieScraperCleanFields AndAlso Not Master.eSettings.MovieScraperGenre AndAlso Not Master.eSettings.MovieLockGenre Then
                tDBMovie.MainDetails.Genres.Clear()
            End If

            'MPAA
            If (Not tDBMovie.MainDetails.MPAASpecified OrElse Not Master.eSettings.MovieLockMPAA) AndAlso tDBMovie.ScrapeOptions.bMainMPAA AndAlso
                scrapedmovie.MPAASpecified AndAlso Master.eSettings.MovieScraperMPAA AndAlso Not new_MPAA Then
                tDBMovie.MainDetails.MPAA = scrapedmovie.MPAA
                new_MPAA = True
            ElseIf Master.eSettings.MovieScraperCleanFields AndAlso Not Master.eSettings.MovieScraperMPAA AndAlso Not Master.eSettings.MovieLockMPAA Then
                tDBMovie.MainDetails.MPAA = String.Empty
            End If

            'Originaltitle
            If (Not tDBMovie.MainDetails.OriginalTitleSpecified OrElse Not Master.eSettings.MovieLockOriginalTitle) AndAlso tDBMovie.ScrapeOptions.bMainOriginalTitle AndAlso
                scrapedmovie.OriginalTitleSpecified AndAlso Master.eSettings.MovieScraperOriginalTitle AndAlso Not new_OriginalTitle Then
                tDBMovie.MainDetails.OriginalTitle = scrapedmovie.OriginalTitle
                new_OriginalTitle = True
            ElseIf Master.eSettings.MovieScraperCleanFields AndAlso Not Master.eSettings.MovieScraperOriginalTitle AndAlso Not Master.eSettings.MovieLockOriginalTitle Then
                tDBMovie.MainDetails.OriginalTitle = String.Empty
            End If

            'Outline
            If (Not tDBMovie.MainDetails.OutlineSpecified OrElse Not Master.eSettings.MovieLockOutline) AndAlso tDBMovie.ScrapeOptions.bMainOutline AndAlso
                scrapedmovie.OutlineSpecified AndAlso Master.eSettings.MovieScraperOutline AndAlso Not new_Outline Then
                tDBMovie.MainDetails.Outline = scrapedmovie.Outline
                new_Outline = True
            ElseIf Master.eSettings.MovieScraperCleanFields AndAlso Not Master.eSettings.MovieScraperOutline AndAlso Not Master.eSettings.MovieLockOutline Then
                tDBMovie.MainDetails.Outline = String.Empty
            End If
            'check if brackets should be removed...
            If Master.eSettings.MovieScraperCleanPlotOutline Then
                tDBMovie.MainDetails.Outline = StringUtils.RemoveBrackets(tDBMovie.MainDetails.Outline)
            End If

            'Plot
            If (Not tDBMovie.MainDetails.PlotSpecified OrElse Not Master.eSettings.MovieLockPlot) AndAlso tDBMovie.ScrapeOptions.bMainPlot AndAlso
                scrapedmovie.PlotSpecified AndAlso Master.eSettings.MovieScraperPlot AndAlso Not new_Plot Then
                tDBMovie.MainDetails.Plot = scrapedmovie.Plot
                new_Plot = True
            ElseIf Master.eSettings.MovieScraperCleanFields AndAlso Not Master.eSettings.MovieScraperPlot AndAlso Not Master.eSettings.MovieLockPlot Then
                tDBMovie.MainDetails.Plot = String.Empty
            End If
            'check if brackets should be removed...
            If Master.eSettings.MovieScraperCleanPlotOutline Then
                tDBMovie.MainDetails.Plot = StringUtils.RemoveBrackets(tDBMovie.MainDetails.Plot)
            End If

            'Rating/Votes
            If (Not tDBMovie.MainDetails.RatingSpecified OrElse Not Master.eSettings.MovieLockRating) AndAlso tDBMovie.ScrapeOptions.bMainRating AndAlso
                scrapedmovie.RatingSpecified AndAlso Master.eSettings.MovieScraperRating AndAlso Not new_Rating Then
                tDBMovie.MainDetails.Rating = scrapedmovie.Rating
                tDBMovie.MainDetails.Votes = NumUtils.CleanVotes(scrapedmovie.Votes)
                new_Rating = True
            ElseIf Master.eSettings.MovieScraperCleanFields AndAlso Not Master.eSettings.MovieScraperRating AndAlso Not Master.eSettings.MovieLockRating Then
                tDBMovie.MainDetails.Rating = String.Empty
                tDBMovie.MainDetails.Votes = String.Empty
            End If

            'ReleaseDate
            If (Not tDBMovie.MainDetails.ReleaseDateSpecified OrElse Not Master.eSettings.MovieLockReleaseDate) AndAlso tDBMovie.ScrapeOptions.bMainRelease AndAlso
                scrapedmovie.ReleaseDateSpecified AndAlso Master.eSettings.MovieScraperRelease AndAlso Not new_ReleaseDate Then
                tDBMovie.MainDetails.ReleaseDate = NumUtils.DateToISO8601Date(scrapedmovie.ReleaseDate)
                new_ReleaseDate = True
            ElseIf Master.eSettings.MovieScraperCleanFields AndAlso Not Master.eSettings.MovieScraperRelease AndAlso Not Master.eSettings.MovieLockReleaseDate Then
                tDBMovie.MainDetails.ReleaseDate = String.Empty
            End If

            'Studios
            If (Not tDBMovie.MainDetails.StudiosSpecified OrElse Not Master.eSettings.MovieLockStudio) AndAlso tDBMovie.ScrapeOptions.bMainStudios AndAlso
                scrapedmovie.StudiosSpecified AndAlso Master.eSettings.MovieScraperStudio AndAlso Not new_Studio Then
                tDBMovie.MainDetails.Studios.Clear()

                Dim _studios As New List(Of String)
                _studios.AddRange(scrapedmovie.Studios)

                If Master.eSettings.MovieScraperStudioWithImgOnly Then
                    For i = _studios.Count - 1 To 0 Step -1
                        If APIXML.dStudios.ContainsKey(_studios.Item(i).ToLower) = False Then
                            _studios.RemoveAt(i)
                        End If
                    Next
                End If

                If Master.eSettings.MovieScraperStudioLimit > 0 AndAlso Master.eSettings.MovieScraperStudioLimit < _studios.Count AndAlso _studios.Count > 0 Then
                    _studios.RemoveRange(Master.eSettings.MovieScraperStudioLimit, _studios.Count - Master.eSettings.MovieScraperStudioLimit)
                End If


                tDBMovie.MainDetails.Studios.AddRange(_studios)
                'added check if there's any studios left to add, if not then try with results of following scraper...
                If _studios.Count > 0 Then
                    new_Studio = True
                End If


            ElseIf Master.eSettings.MovieScraperCleanFields AndAlso Not Master.eSettings.MovieScraperStudio AndAlso Not Master.eSettings.MovieLockStudio Then
                tDBMovie.MainDetails.Studios.Clear()
            End If

            'Tagline
            If (Not tDBMovie.MainDetails.TaglineSpecified OrElse Not Master.eSettings.MovieLockTagline) AndAlso tDBMovie.ScrapeOptions.bMainTagline AndAlso
                scrapedmovie.TaglineSpecified AndAlso Master.eSettings.MovieScraperTagline AndAlso Not new_Tagline Then
                tDBMovie.MainDetails.Tagline = scrapedmovie.Tagline
                new_Tagline = True
            ElseIf Master.eSettings.MovieScraperCleanFields AndAlso Not Master.eSettings.MovieScraperTagline AndAlso Not Master.eSettings.MovieLockTagline Then
                tDBMovie.MainDetails.Tagline = String.Empty
            End If

            'Title
            If (Not tDBMovie.MainDetails.TitleSpecified OrElse Not Master.eSettings.MovieLockTitle) AndAlso tDBMovie.ScrapeOptions.bMainTitle AndAlso
                scrapedmovie.TitleSpecified AndAlso Master.eSettings.MovieScraperTitle AndAlso Not new_Title Then
                tDBMovie.MainDetails.Title = scrapedmovie.Title
                new_Title = True
            ElseIf Master.eSettings.MovieScraperCleanFields AndAlso Not Master.eSettings.MovieScraperTitle AndAlso Not Master.eSettings.MovieLockTitle Then
                tDBMovie.MainDetails.Title = String.Empty
            End If

            'Top250 (special handling: no check if "scrapedmovie.Top250Specified" and only set "new_Top250 = True" if a value over 0 has been set)
            If (Not tDBMovie.MainDetails.Top250Specified OrElse Not Master.eSettings.MovieLockTop250) AndAlso tDBMovie.ScrapeOptions.bMainTop250 AndAlso
                Master.eSettings.MovieScraperTop250 AndAlso Not new_Top250 Then
                tDBMovie.MainDetails.Top250 = scrapedmovie.Top250
                new_Top250 = If(scrapedmovie.Top250Specified, True, False)
            ElseIf Master.eSettings.MovieScraperCleanFields AndAlso Not Master.eSettings.MovieScraperTop250 AndAlso Not Master.eSettings.MovieLockTop250 Then
                tDBMovie.MainDetails.Top250 = 0
            End If

            'Trailer
            If (Not tDBMovie.MainDetails.TrailerSpecified OrElse Not Master.eSettings.MovieLockTrailer) AndAlso tDBMovie.ScrapeOptions.bMainTrailer AndAlso
                scrapedmovie.TrailerSpecified AndAlso Master.eSettings.MovieScraperTrailer AndAlso Not new_Trailer Then
                If Master.eSettings.MovieScraperXBMCTrailerFormat AndAlso YouTube.UrlUtils.IsYouTubeURL(scrapedmovie.Trailer) Then
                    tDBMovie.MainDetails.Trailer = StringUtils.ConvertFromYouTubeURLToKodiTrailerFormat(scrapedmovie.Trailer)
                Else
                    tDBMovie.MainDetails.Trailer = scrapedmovie.Trailer
                End If
                new_Trailer = True
            ElseIf Master.eSettings.MovieScraperCleanFields AndAlso Not Master.eSettings.MovieScraperTrailer AndAlso Not Master.eSettings.MovieLockTrailer Then
                tDBMovie.MainDetails.Trailer = String.Empty
            End If

            'UserRating
            If (Not tDBMovie.MainDetails.UserRatingSpecified OrElse Not Master.eSettings.MovieLockUserRating) AndAlso tDBMovie.ScrapeOptions.bMainUserRating AndAlso
                scrapedmovie.UserRatingSpecified AndAlso Master.eSettings.MovieScraperUserRating AndAlso Not new_UserRating Then
                tDBMovie.MainDetails.UserRating = scrapedmovie.UserRating
                new_Rating = True
            ElseIf Master.eSettings.MovieScraperCleanFields AndAlso Not Master.eSettings.MovieScraperUserRating AndAlso Not Master.eSettings.MovieLockUserRating Then
                tDBMovie.MainDetails.UserRating = 0
            End If

            'Year
            If (Not tDBMovie.MainDetails.YearSpecified OrElse Not Master.eSettings.MovieLockYear) AndAlso tDBMovie.ScrapeOptions.bMainYear AndAlso
                scrapedmovie.YearSpecified AndAlso Master.eSettings.MovieScraperYear AndAlso Not new_Year Then
                tDBMovie.MainDetails.Year = scrapedmovie.Year
                new_Year = True
            ElseIf Master.eSettings.MovieScraperCleanFields AndAlso Not Master.eSettings.MovieScraperYear AndAlso Not Master.eSettings.MovieLockYear Then
                tDBMovie.MainDetails.Year = String.Empty
            End If

            'Runtime
            If (Not tDBMovie.MainDetails.RuntimeSpecified OrElse Not Master.eSettings.MovieLockRuntime) AndAlso tDBMovie.ScrapeOptions.bMainRuntime AndAlso
                scrapedmovie.RuntimeSpecified AndAlso Master.eSettings.MovieScraperRuntime AndAlso Not new_Runtime Then
                tDBMovie.MainDetails.Runtime = scrapedmovie.Runtime
                new_Runtime = True
            ElseIf Master.eSettings.MovieScraperCleanFields AndAlso Not Master.eSettings.MovieScraperRuntime AndAlso Not Master.eSettings.MovieLockRuntime Then
                tDBMovie.MainDetails.Runtime = String.Empty
            End If

        Next

        'Certification for MPAA
        If tDBMovie.MainDetails.CertificationsSpecified AndAlso Master.eSettings.MovieScraperCertForMPAA AndAlso
            (Not Master.eSettings.MovieScraperCertForMPAAFallback AndAlso (Not tDBMovie.MainDetails.MPAASpecified OrElse Not Master.eSettings.MovieLockMPAA) OrElse
             Not new_MPAA AndAlso (Not tDBMovie.MainDetails.MPAASpecified OrElse Not Master.eSettings.MovieLockMPAA)) Then

            Dim tmpstring As String = String.Empty
            tmpstring = If(Master.eSettings.MovieScraperCertLang = "us", StringUtils.USACertToMPAA(String.Join(" / ", tDBMovie.MainDetails.Certifications.ToArray)), If(Master.eSettings.MovieScraperCertOnlyValue, String.Join(" / ", tDBMovie.MainDetails.Certifications.ToArray).Split(Convert.ToChar(":"))(1), String.Join(" / ", tDBMovie.MainDetails.Certifications.ToArray)))
            'only update DBMovie if scraped result is not empty/nothing!
            If Not String.IsNullOrEmpty(tmpstring) Then
                tDBMovie.MainDetails.MPAA = tmpstring
            End If
        End If

        'MPAA value if MPAA is not available
        If Not tDBMovie.MainDetails.MPAASpecified AndAlso Not String.IsNullOrEmpty(Master.eSettings.MovieScraperMPAANotRated) Then
            tDBMovie.MainDetails.MPAA = Master.eSettings.MovieScraperMPAANotRated
        End If

        'Plot for Outline
        If ((Not tDBMovie.MainDetails.OutlineSpecified OrElse Not Master.eSettings.MovieLockOutline) AndAlso Master.eSettings.MovieScraperPlotForOutline AndAlso Not Master.eSettings.MovieScraperPlotForOutlineIfEmpty) OrElse
            (Not tDBMovie.MainDetails.OutlineSpecified AndAlso Master.eSettings.MovieScraperPlotForOutline AndAlso Master.eSettings.MovieScraperPlotForOutlineIfEmpty) Then
            tDBMovie.MainDetails.Outline = StringUtils.ShortenOutline(tDBMovie.MainDetails.Plot, Master.eSettings.MovieScraperOutlineLimit)
        End If

        'set ListTitle at the end of merging
        If tDBMovie.MainDetails.TitleSpecified Then
            Dim tTitle As String = StringUtils.SortTokens_Movie(tDBMovie.MainDetails.Title)
            If Master.eSettings.MovieDisplayYear AndAlso Not String.IsNullOrEmpty(tDBMovie.MainDetails.Year) Then
                tDBMovie.ListTitle = String.Format("{0} ({1})", tTitle, tDBMovie.MainDetails.Year)
            Else
                tDBMovie.ListTitle = tTitle
            End If
        Else
            tDBMovie.ListTitle = StringUtils.FilterTitleFromPath_Movie(tDBMovie.Filename, tDBMovie.IsSingle, tDBMovie.Source.UseFolderName)
        End If

        Return tDBMovie
    End Function

    Public Shared Function MergeDataScraperResults_MovieSet(ByVal tDBMovieSet As Database.DBElement, ByVal ScrapedList As List(Of MediaContainers.MainDetails)) As Database.DBElement

        'protects the first scraped result against overwriting
        Dim new_Plot As Boolean = False
        Dim new_Title As Boolean = False

        For Each scrapedmovieset In ScrapedList

            'IDs
            If scrapedmovieset.TMDBSpecified Then
                tDBMovieSet.MainDetails.TMDB = scrapedmovieset.TMDB
            End If

            'Plot
            If (Not tDBMovieSet.MainDetails.PlotSpecified OrElse Not Master.eSettings.MovieSetLockPlot) AndAlso tDBMovieSet.ScrapeOptions.bMainPlot AndAlso
                scrapedmovieset.PlotSpecified AndAlso Master.eSettings.MovieSetScraperPlot AndAlso Not new_Plot Then
                tDBMovieSet.MainDetails.Plot = scrapedmovieset.Plot
                new_Plot = True
                'ElseIf Master.eSettings.MovieSetScraperCleanFields AndAlso Not Master.eSettings.MovieSetScraperPlot AndAlso Not Master.eSettings.MovieSetLockPlot Then
                '    DBMovieSet.MovieSet.Plot = String.Empty
            End If

            'Title
            If (Not tDBMovieSet.MainDetails.TitleSpecified OrElse Not Master.eSettings.MovieSetLockTitle) AndAlso tDBMovieSet.ScrapeOptions.bMainTitle AndAlso
                 scrapedmovieset.TitleSpecified AndAlso Master.eSettings.MovieSetScraperTitle AndAlso Not new_Title Then
                tDBMovieSet.MainDetails.Title = scrapedmovieset.Title
                new_Title = True
                'ElseIf Master.eSettings.MovieSetScraperCleanFields AndAlso Not Master.eSettings.MovieSetScraperTitle AndAlso Not Master.eSettings.MovieSetLockTitle Then
                '    DBMovieSet.MovieSet.Title = String.Empty
            End If
        Next

        'set Title
        For Each sett As AdvancedSettingsSetting In clsXMLAdvancedSettings.GetAllSettings.Where(Function(y) y.Name.StartsWith("MovieSetTitleRenamer:"))
            tDBMovieSet.MainDetails.Title = tDBMovieSet.MainDetails.Title.Replace(sett.Name.Substring(21), sett.Value)
        Next

        'set ListTitle at the end of merging
        If tDBMovieSet.MainDetails.TitleSpecified Then
            Dim tTitle As String = StringUtils.SortTokens_MovieSet(tDBMovieSet.MainDetails.Title)
            tDBMovieSet.ListTitle = tTitle
        Else
            'If FileUtils.Common.isVideoTS(DBMovie.Filename) Then
            '    DBMovie.ListTitle = StringUtils.FilterName_Movie(Directory.GetParent(Directory.GetParent(DBMovie.Filename).FullName).Name)
            'ElseIf FileUtils.Common.isBDRip(DBMovie.Filename) Then
            '    DBMovie.ListTitle = StringUtils.FilterName_Movie(Directory.GetParent(Directory.GetParent(Directory.GetParent(DBMovie.Filename).FullName).FullName).Name)
            'Else
            '    If DBMovie.UseFolder AndAlso DBMovie.IsSingle Then
            '        DBMovie.ListTitle = StringUtils.FilterName_Movie(Directory.GetParent(DBMovie.Filename).Name)
            '    Else
            '        DBMovie.ListTitle = StringUtils.FilterName_Movie(Path.GetFileNameWithoutExtension(DBMovie.Filename))
            '    End If
            'End If
        End If

        Return tDBMovieSet
    End Function
    ''' <summary>
    ''' Returns the "merged" result of each data scraper results
    ''' </summary>
    ''' <param name="tDBTV">TV Show to be scraped</param>
    ''' <param name="ScrapedList"><c>List(Of MediaContainers.MainDetails)</c> which contains unfiltered results of each data scraper</param>
    ''' <returns>The scrape result of movie (after applying various global scraper settings here)</returns>
    ''' <remarks>
    ''' This is used to determine the result of data scraping by going through all scraperesults of every data scraper and applying global data scraper settings here!
    ''' 
    ''' 2014/09/01 Cocotus - First implementation: Moved all global lock settings in various data scrapers to this function, only apply them once and not in every data scraper module! Should be more maintainable!
    ''' </remarks>
    Public Shared Function MergeDataScraperResults_TV(ByVal tDBTV As Database.DBElement, ByVal ScrapedList As List(Of MediaContainers.MainDetails)) As Database.DBElement

        'protects the first scraped result against overwriting
        Dim new_Actors As Boolean = False
        Dim new_Certification As Boolean = False
        Dim new_Collections As Boolean = False
        Dim new_Creators As Boolean = False
        Dim new_Credits As Boolean = False
        Dim new_Directors As Boolean = False
        Dim new_Genres As Boolean = False
        Dim new_MPAA As Boolean = False
        Dim new_OriginalTitle As Boolean = False
        Dim new_Outline As Boolean = False
        Dim new_Plot As Boolean = False
        Dim new_Premiered As Boolean = False
        Dim new_Rating As Boolean = False
        Dim new_Runtime As Boolean = False
        Dim new_ShowCountries As Boolean = False
        Dim new_Status As Boolean = False
        Dim new_Studio As Boolean = False
        Dim new_Tagline As Boolean = False
        Dim new_Title As Boolean = False
        Dim new_Trailer As Boolean = False
        Dim new_UserRating As Boolean = False

        Dim KnownEpisodesIndex As New List(Of KnownEpisode)
        Dim KnownSeasonsIndex As New List(Of Integer)

        ''If "Use Preview Datascraperresults" option is enabled, a preview window which displays all datascraperresults will be opened before showing the Edit Movie page!
        'If (ScrapeType = Enums.ScrapeType_Movie_MovieSet_TV.SingleScrape OrElse ScrapeType = Enums.ScrapeType_Movie_MovieSet_TV.SingleField) AndAlso Master.eSettings.MovieScraperUseDetailView AndAlso ScrapedList.Count > 0 Then
        '    PreviewDataScraperResults(ScrapedList)
        'End If

        For Each scrapedshow In ScrapedList

            'IDs
            If scrapedshow.TVDBSpecified Then
                tDBTV.MainDetails.TVDB = scrapedshow.TVDB
            End If
            If scrapedshow.IMDBSpecified Then
                tDBTV.MainDetails.IMDB = scrapedshow.IMDB
            End If
            If scrapedshow.TMDBSpecified Then
                tDBTV.MainDetails.TMDB = scrapedshow.TMDB
            End If

            'Actors
            If (Not tDBTV.MainDetails.ActorsSpecified OrElse Not Master.eSettings.TVLockShowActors) AndAlso tDBTV.ScrapeOptions.bMainActors AndAlso
                scrapedshow.ActorsSpecified AndAlso Master.eSettings.TVScraperShowActors AndAlso Not new_Actors Then

                'If Master.eSettings.MovieScraperCastWithImgOnly Then
                '    For i = scrapedmovie.Actors.Count - 1 To 0 Step -1
                '        If String.IsNullOrEmpty(scrapedmovie.Actors(i).ThumbURL) Then
                '            scrapedmovie.Actors.RemoveAt(i)
                '        End If
                '    Next
                'End If

                'If Master.eSettings.MovieScraperCastLimit > 0 AndAlso scrapedmovie.Actors.Count > Master.eSettings.MovieScraperCastLimit Then
                '    scrapedmovie.Actors.RemoveRange(Master.eSettings.MovieScraperCastLimit, scrapedmovie.Actors.Count - Master.eSettings.MovieScraperCastLimit)
                'End If

                tDBTV.MainDetails.Actors = scrapedshow.Actors
                'added check if there's any actors left to add, if not then try with results of following scraper...
                If scrapedshow.ActorsSpecified Then
                    new_Actors = True
                    'add numbers for ordering
                    Dim iOrder As Integer = 0
                    For Each actor In scrapedshow.Actors
                        actor.Order = iOrder
                        iOrder += 1
                    Next
                End If

            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperShowActors AndAlso Not Master.eSettings.TVLockShowActors Then
                tDBTV.MainDetails.Actors.Clear()
            End If

            'Certification
            If (Not tDBTV.MainDetails.CertificationsSpecified OrElse Not Master.eSettings.TVLockShowCert) AndAlso tDBTV.ScrapeOptions.bMainCertifications AndAlso
                scrapedshow.CertificationsSpecified AndAlso Master.eSettings.TVScraperShowCert AndAlso Not new_Certification Then
                If Master.eSettings.TVScraperShowCertLang = Master.eLang.All Then
                    tDBTV.MainDetails.Certifications = scrapedshow.Certifications
                    new_Certification = True
                Else
                    Dim CertificationLanguage = APIXML.CertLanguagesXML.Language.FirstOrDefault(Function(l) l.abbreviation = Master.eSettings.TVScraperShowCertLang)
                    If CertificationLanguage IsNot Nothing AndAlso CertificationLanguage.name IsNot Nothing AndAlso Not String.IsNullOrEmpty(CertificationLanguage.name) Then
                        For Each tCert In scrapedshow.Certifications
                            If tCert.StartsWith(CertificationLanguage.name) Then
                                tDBTV.MainDetails.Certifications.Clear()
                                tDBTV.MainDetails.Certifications.Add(tCert)
                                new_Certification = True
                                Exit For
                            End If
                        Next
                    Else
                        logger.Error("TV Show Certification Language (Limit) not found. Please check your settings!")
                    End If
                End If
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperShowCert AndAlso Not Master.eSettings.TVLockShowCert Then
                tDBTV.MainDetails.Certifications.Clear()
            End If

            'Creators
            If (Not tDBTV.MainDetails.CreatorsSpecified OrElse Not Master.eSettings.TVLockShowCreators) AndAlso tDBTV.ScrapeOptions.bMainCreators AndAlso
                scrapedshow.CreatorsSpecified AndAlso Master.eSettings.TVScraperShowCreators AndAlso Not new_Creators Then
                tDBTV.MainDetails.Creators = scrapedshow.Creators
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperShowCreators AndAlso Not Master.eSettings.TVLockShowCreators Then
                tDBTV.MainDetails.Creators.Clear()
            End If

            'Countries
            If (Not tDBTV.MainDetails.CountriesSpecified OrElse Not Master.eSettings.TVLockShowCountry) AndAlso tDBTV.ScrapeOptions.bMainCountries AndAlso
                scrapedshow.CountriesSpecified AndAlso Master.eSettings.TVScraperShowCountry AndAlso Not new_ShowCountries Then
                tDBTV.MainDetails.Countries = scrapedshow.Countries
                new_ShowCountries = True
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperShowCountry AndAlso Not Master.eSettings.TVLockShowCountry Then
                tDBTV.MainDetails.Countries.Clear()
            End If

            'EpisodeGuideURL
            If tDBTV.ScrapeOptions.bMainEpisodeGuide AndAlso scrapedshow.EpisodeGuideSpecified AndAlso Master.eSettings.TVScraperShowEpiGuideURL Then
                tDBTV.MainDetails.EpisodeGuide = scrapedshow.EpisodeGuide
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperShowEpiGuideURL Then
                tDBTV.MainDetails.EpisodeGuide.Clear()
            End If

            'Genres
            If (Not tDBTV.MainDetails.GenresSpecified OrElse Not Master.eSettings.TVLockShowGenre) AndAlso tDBTV.ScrapeOptions.bMainGenres AndAlso
                scrapedshow.GenresSpecified AndAlso Master.eSettings.TVScraperShowGenre AndAlso Not new_Genres Then

                StringUtils.GenreFilter(scrapedshow.Genres)

                'If Master.eSettings.TVScraperShowGenreLimit > 0 AndAlso Master.eSettings.TVScraperShowGenreLimit < _genres.Count AndAlso _genres.Count > 0 Then
                '    _genres.RemoveRange(Master.eSettings.TVScraperShowGenreLimit, _genres.Count - Master.eSettings.TVScraperShowGenreLimit)
                'End If
                tDBTV.MainDetails.Genres = scrapedshow.Genres
                new_Genres = True
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperShowGenre AndAlso Not Master.eSettings.TVLockShowGenre Then
                tDBTV.MainDetails.Genres.Clear()
            End If

            'MPAA
            If (Not tDBTV.MainDetails.MPAASpecified OrElse Not Master.eSettings.TVLockShowMPAA) AndAlso tDBTV.ScrapeOptions.bMainMPAA AndAlso
              scrapedshow.MPAASpecified AndAlso Master.eSettings.TVScraperShowMPAA AndAlso Not new_MPAA Then
                tDBTV.MainDetails.MPAA = scrapedshow.MPAA
                new_MPAA = True
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperShowMPAA AndAlso Not Master.eSettings.TVLockShowMPAA Then
                tDBTV.MainDetails.MPAA = String.Empty
            End If

            'Originaltitle
            If (Not tDBTV.MainDetails.OriginalTitleSpecified OrElse Not Master.eSettings.TVLockShowOriginalTitle) AndAlso tDBTV.ScrapeOptions.bMainOriginalTitle AndAlso
                scrapedshow.OriginalTitleSpecified AndAlso Master.eSettings.TVScraperShowOriginalTitle AndAlso Not new_OriginalTitle Then
                tDBTV.MainDetails.OriginalTitle = scrapedshow.OriginalTitle
                new_OriginalTitle = True
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperShowOriginalTitle AndAlso Not Master.eSettings.TVLockShowOriginalTitle Then
                tDBTV.MainDetails.OriginalTitle = String.Empty
            End If

            'Plot
            If (Not tDBTV.MainDetails.PlotSpecified OrElse Not Master.eSettings.TVLockShowPlot) AndAlso tDBTV.ScrapeOptions.bMainPlot AndAlso
                 scrapedshow.PlotSpecified AndAlso Master.eSettings.TVScraperShowPlot AndAlso Not new_Plot Then
                tDBTV.MainDetails.Plot = scrapedshow.Plot
                new_Plot = True
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperShowPlot AndAlso Not Master.eSettings.TVLockShowPlot Then
                tDBTV.MainDetails.Plot = String.Empty
            End If

            'Premiered
            If (Not tDBTV.MainDetails.PremieredSpecified OrElse Not Master.eSettings.TVLockShowPremiered) AndAlso tDBTV.ScrapeOptions.bMainPremiered AndAlso
                scrapedshow.PremieredSpecified AndAlso Master.eSettings.TVScraperShowPremiered AndAlso Not new_Premiered Then
                tDBTV.MainDetails.Premiered = NumUtils.DateToISO8601Date(scrapedshow.Premiered)
                new_Premiered = True
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperShowPremiered AndAlso Not Master.eSettings.TVLockShowPremiered Then
                tDBTV.MainDetails.Premiered = String.Empty
            End If

            'Rating/Votes
            If (Not tDBTV.MainDetails.RatingSpecified OrElse Not Master.eSettings.TVLockShowRating) AndAlso tDBTV.ScrapeOptions.bMainRating AndAlso
                scrapedshow.RatingSpecified AndAlso Master.eSettings.TVScraperShowRating AndAlso Not new_Rating Then
                tDBTV.MainDetails.Rating = scrapedshow.Rating
                tDBTV.MainDetails.Votes = NumUtils.CleanVotes(scrapedshow.Votes)
                new_Rating = True
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperShowRating AndAlso Not Master.eSettings.TVLockShowRating Then
                tDBTV.MainDetails.Rating = String.Empty
                tDBTV.MainDetails.Votes = String.Empty
            End If

            'Runtime
            If (Not tDBTV.MainDetails.RuntimeSpecified OrElse tDBTV.MainDetails.Runtime = "0" OrElse Not Master.eSettings.TVLockShowRuntime) AndAlso tDBTV.ScrapeOptions.bMainRuntime AndAlso
                scrapedshow.RuntimeSpecified AndAlso Not scrapedshow.Runtime = "0" AndAlso Master.eSettings.TVScraperShowRuntime AndAlso Not new_Runtime Then
                tDBTV.MainDetails.Runtime = scrapedshow.Runtime
                new_Runtime = True
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperShowRuntime AndAlso Not Master.eSettings.TVLockShowRuntime Then
                tDBTV.MainDetails.Runtime = String.Empty
            End If

            'Status
            If (tDBTV.MainDetails.StatusSpecified OrElse Not Master.eSettings.TVLockShowStatus) AndAlso tDBTV.ScrapeOptions.bMainStatus AndAlso
                scrapedshow.StatusSpecified AndAlso Master.eSettings.TVScraperShowStatus AndAlso Not new_Status Then
                tDBTV.MainDetails.Status = scrapedshow.Status
                new_Status = True
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperShowStatus AndAlso Not Master.eSettings.TVLockShowStatus Then
                tDBTV.MainDetails.Status = String.Empty
            End If

            'Studios
            If (Not tDBTV.MainDetails.StudiosSpecified OrElse Not Master.eSettings.TVLockShowStudio) AndAlso tDBTV.ScrapeOptions.bMainStudios AndAlso
                scrapedshow.StudiosSpecified AndAlso Master.eSettings.TVScraperShowStudio AndAlso Not new_Studio Then
                tDBTV.MainDetails.Studios.Clear()

                Dim _studios As New List(Of String)
                _studios.AddRange(scrapedshow.Studios)

                'If Master.eSettings.TVScraperShowStudioWithImgOnly Then
                '    For i = _studios.Count - 1 To 0 Step -1
                '        If APIXML.dStudios.ContainsKey(_studios.Item(i).ToLower) = False Then
                '            _studios.RemoveAt(i)
                '        End If
                '    Next
                'End If

                'If Master.eSettings.tvScraperStudioLimit > 0 AndAlso Master.eSettings.MovieScraperStudioLimit < _studios.Count AndAlso _studios.Count > 0 Then
                '    _studios.RemoveRange(Master.eSettings.MovieScraperStudioLimit, _studios.Count - Master.eSettings.MovieScraperStudioLimit)
                'End If


                tDBTV.MainDetails.Studios.AddRange(_studios)
                'added check if there's any studios left to add, if not then try with results of following scraper...
                If _studios.Count > 0 Then
                    new_Studio = True
                End If


            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperShowStudio AndAlso Not Master.eSettings.TVLockShowStudio Then
                tDBTV.MainDetails.Studios.Clear()
            End If

            'Title
            If (Not tDBTV.MainDetails.TitleSpecified OrElse Not Master.eSettings.TVLockShowTitle) AndAlso tDBTV.ScrapeOptions.bMainTitle AndAlso
                scrapedshow.TitleSpecified AndAlso Master.eSettings.TVScraperShowTitle AndAlso Not new_Title Then
                tDBTV.MainDetails.Title = scrapedshow.Title
                new_Title = True
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperShowTitle AndAlso Not Master.eSettings.TVLockShowTitle Then
                tDBTV.MainDetails.Title = String.Empty
            End If

            'UserRating
            If (Not tDBTV.MainDetails.UserRatingSpecified OrElse Not Master.eSettings.TVLockShowUserRating) AndAlso tDBTV.ScrapeOptions.bMainUserRating AndAlso
                scrapedshow.UserRatingSpecified AndAlso Master.eSettings.TVScraperShowUserRating AndAlso Not new_UserRating Then
                tDBTV.MainDetails.UserRating = scrapedshow.UserRating
                new_UserRating = True
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperShowUserRating AndAlso Not Master.eSettings.TVLockShowUserRating Then
                tDBTV.MainDetails.UserRating = 0
            End If

            '    'Credits
            '    If (DBTV.Movie.Credits.Count < 1 OrElse Not Master.eSettings.MovieLockCredits) AndAlso _
            '        scrapedmovie.Credits.Count > 0 AndAlso Master.eSettings.MovieScraperCredits AndAlso Not new_Credits Then
            '        DBTV.Movie.Credits.Clear()
            '        DBTV.Movie.Credits.AddRange(scrapedmovie.Credits)
            '        new_Credits = True
            '    ElseIf Master.eSettings.MovieScraperCleanFields AndAlso Not Master.eSettings.MovieScraperCredits AndAlso Not Master.eSettings.MovieLockCredits Then
            '        DBTV.Movie.Credits.Clear()
            '    End If

            'Create KnowSeasons index
            For Each kSeason As MediaContainers.MainDetails In scrapedshow.KnownSeasons
                If Not KnownSeasonsIndex.Contains(kSeason.Season) Then
                    KnownSeasonsIndex.Add(kSeason.Season)
                End If
            Next

            'Create KnownEpisodes index (season and episode number)
            If tDBTV.ScrapeModifiers.withEpisodes Then
                For Each kEpisode As MediaContainers.MainDetails In scrapedshow.KnownEpisodes
                    Dim nKnownEpisode As New KnownEpisode With {.AiredDate = kEpisode.Aired,
                                                                .Episode = kEpisode.Episode,
                                                                .EpisodeAbsolute = kEpisode.EpisodeAbsolute,
                                                                .EpisodeCombined = kEpisode.EpisodeCombined,
                                                                .EpisodeDVD = kEpisode.EpisodeDVD,
                                                                .Season = kEpisode.Season,
                                                                .SeasonCombined = kEpisode.SeasonCombined,
                                                                .SeasonDVD = kEpisode.SeasonDVD}
                    If KnownEpisodesIndex.Where(Function(f) f.Episode = nKnownEpisode.Episode AndAlso f.Season = nKnownEpisode.Season).Count = 0 Then
                        KnownEpisodesIndex.Add(nKnownEpisode)

                        'try to get an episode information with more numbers
                    ElseIf KnownEpisodesIndex.Where(Function(f) f.Episode = nKnownEpisode.Episode AndAlso f.Season = nKnownEpisode.Season AndAlso
                                ((nKnownEpisode.EpisodeAbsolute > -1 AndAlso Not f.EpisodeAbsolute = nKnownEpisode.EpisodeAbsolute) OrElse
                                 (nKnownEpisode.EpisodeCombined > -1 AndAlso Not f.EpisodeCombined = nKnownEpisode.EpisodeCombined) OrElse
                                 (nKnownEpisode.EpisodeDVD > -1 AndAlso Not f.EpisodeDVD = nKnownEpisode.EpisodeDVD) OrElse
                                 (nKnownEpisode.SeasonCombined > -1 AndAlso Not f.SeasonCombined = nKnownEpisode.SeasonCombined) OrElse
                                 (nKnownEpisode.SeasonDVD > -1 AndAlso Not f.SeasonDVD = nKnownEpisode.SeasonDVD))).Count = 1 Then
                        Dim toRemove As KnownEpisode = KnownEpisodesIndex.FirstOrDefault(Function(f) f.Episode = nKnownEpisode.Episode AndAlso f.Season = nKnownEpisode.Season)
                        KnownEpisodesIndex.Remove(toRemove)
                        KnownEpisodesIndex.Add(nKnownEpisode)
                    End If
                Next
            End If
        Next

        'Certification for MPAA
        If tDBTV.MainDetails.CertificationsSpecified AndAlso Master.eSettings.TVScraperShowCertForMPAA AndAlso
            (Not Master.eSettings.MovieScraperCertForMPAAFallback AndAlso (Not tDBTV.MainDetails.MPAASpecified OrElse Not Master.eSettings.TVLockShowMPAA) OrElse
             Not new_MPAA AndAlso (Not tDBTV.MainDetails.MPAASpecified OrElse Not Master.eSettings.TVLockShowMPAA)) Then

            Dim tmpstring As String = String.Empty
            tmpstring = If(Master.eSettings.TVScraperShowCertLang = "us", StringUtils.USACertToMPAA(String.Join(" / ", tDBTV.MainDetails.Certifications.ToArray)), If(Master.eSettings.TVScraperShowCertOnlyValue, String.Join(" / ", tDBTV.MainDetails.Certifications.ToArray).Split(Convert.ToChar(":"))(1), String.Join(" / ", tDBTV.MainDetails.Certifications.ToArray)))
            'only update DBMovie if scraped result is not empty/nothing!
            If Not String.IsNullOrEmpty(tmpstring) Then
                tDBTV.MainDetails.MPAA = tmpstring
            End If
        End If

        'MPAA value if MPAA is not available
        If Not tDBTV.MainDetails.MPAASpecified AndAlso Not String.IsNullOrEmpty(Master.eSettings.TVScraperShowMPAANotRated) Then
            tDBTV.MainDetails.MPAA = Master.eSettings.TVScraperShowMPAANotRated
        End If

        'set ListTitle at the end of merging
        If tDBTV.MainDetails.TitleSpecified Then
            tDBTV.ListTitle = StringUtils.SortTokens_TV(tDBTV.MainDetails.Title)
        End If


        'Seasons
        For Each aKnownSeason As Integer In KnownSeasonsIndex
            'create a list of specified episode informations from all scrapers
            Dim ScrapedSeasonList As New List(Of MediaContainers.MainDetails)
            For Each nShow As MediaContainers.MainDetails In ScrapedList
                For Each nSeasonDetails As MediaContainers.MainDetails In nShow.KnownSeasons.Where(Function(f) f.Season = aKnownSeason)
                    ScrapedSeasonList.Add(nSeasonDetails)
                Next
            Next
            'check if we have already saved season information for this scraped season
            Dim lSeasonList = tDBTV.Seasons.Where(Function(f) f.MainDetails.Season = aKnownSeason)

            If lSeasonList IsNot Nothing AndAlso lSeasonList.Count > 0 Then
                For Each nSeason As Database.DBElement In lSeasonList
                    MergeDataScraperResults_TVSeason(nSeason, ScrapedSeasonList)
                Next
            Else
                'no existing season found -> add it as "missing" season
                Dim mSeason As New Database.DBElement(Enums.ContentType.TVSeason) With {.MainDetails = New MediaContainers.MainDetails With {.Season = aKnownSeason}}
                mSeason = Master.DB.AddTVShowInfoToDBElement(mSeason, tDBTV)
                tDBTV.Seasons.Add(MergeDataScraperResults_TVSeason(mSeason, ScrapedSeasonList))
            End If
        Next
        'add all season informations to TVShow (for saving season informations to tv show NFO)
        tDBTV.MainDetails.Seasons.Seasons.Clear()
        For Each kSeason As Database.DBElement In tDBTV.Seasons.OrderBy(Function(f) f.MainDetails.Season)
            tDBTV.MainDetails.Seasons.Seasons.Add(kSeason.MainDetails)
        Next

        'Episodes
        If tDBTV.ScrapeModifiers.withEpisodes Then
            'update the tvshow information for each local episode
            For Each lEpisode In tDBTV.Episodes
                lEpisode = Master.DB.AddTVShowInfoToDBElement(lEpisode, tDBTV)
                lEpisode.ScrapeModifiers = tDBTV.ScrapeModifiers
                lEpisode.ScrapeOptions = tDBTV.ScrapeOptions
            Next

            For Each aKnownEpisode As KnownEpisode In KnownEpisodesIndex.OrderBy(Function(f) f.Episode).OrderBy(Function(f) f.Season)

                'convert the episode and season number if needed
                Dim iEpisode As Integer = -1
                Dim iSeason As Integer = -1
                Dim strAiredDate As String = aKnownEpisode.AiredDate
                If tDBTV.Ordering = Enums.EpisodeOrdering.Absolute Then
                    iEpisode = aKnownEpisode.EpisodeAbsolute
                    iSeason = 1
                ElseIf tDBTV.Ordering = Enums.EpisodeOrdering.DVD Then
                    iEpisode = CInt(aKnownEpisode.EpisodeDVD)
                    iSeason = aKnownEpisode.SeasonDVD
                ElseIf tDBTV.Ordering = Enums.EpisodeOrdering.Standard Then
                    iEpisode = aKnownEpisode.Episode
                    iSeason = aKnownEpisode.Season
                End If

                If Not iEpisode = -1 AndAlso Not iSeason = -1 Then
                    'create a list of specified episode informations from all scrapers
                    Dim ScrapedEpisodeList As New List(Of MediaContainers.MainDetails)
                    For Each nShow As MediaContainers.MainDetails In ScrapedList
                        For Each nEpisodeDetails As MediaContainers.MainDetails In nShow.KnownEpisodes.Where(Function(f) f.Episode = aKnownEpisode.Episode AndAlso f.Season = aKnownEpisode.Season)
                            ScrapedEpisodeList.Add(nEpisodeDetails)
                        Next
                    Next

                    'check if we have a local episode file for this scraped episode
                    Dim lEpisodeList = tDBTV.Episodes.Where(Function(f) f.FilenameSpecified AndAlso f.MainDetails.Episode = iEpisode AndAlso f.MainDetails.Season = iSeason)

                    If lEpisodeList IsNot Nothing AndAlso lEpisodeList.Count > 0 Then
                        For Each nEpisode As Database.DBElement In lEpisodeList
                            MergeDataScraperResults_TVEpisode(nEpisode, ScrapedEpisodeList)
                        Next
                    Else
                        'try to get the episode by AiredDate
                        Dim dEpisodeList = tDBTV.Episodes.Where(Function(f) f.FilenameSpecified AndAlso
                                                                   f.MainDetails.Episode = -1 AndAlso
                                                                   f.MainDetails.AiredSpecified AndAlso
                                                                   f.MainDetails.Aired = strAiredDate)

                        If dEpisodeList IsNot Nothing AndAlso dEpisodeList.Count > 0 Then
                            For Each nEpisode As Database.DBElement In dEpisodeList
                                MergeDataScraperResults_TVEpisode(nEpisode, ScrapedEpisodeList)
                                'we have to add the proper season and episode number if the episode was found by AiredDate
                                nEpisode.MainDetails.Episode = iEpisode
                                nEpisode.MainDetails.Season = iSeason
                            Next
                        Else
                            'no local episode found -> add it as "missing" episode
                            Dim mEpisode As New Database.DBElement(Enums.ContentType.TVEpisode) With {.MainDetails = New MediaContainers.MainDetails With {.Episode = iEpisode, .Season = iSeason}}
                            mEpisode = Master.DB.AddTVShowInfoToDBElement(mEpisode, tDBTV)
                            MergeDataScraperResults_TVEpisode(mEpisode, ScrapedEpisodeList)
                            If mEpisode.MainDetails.TitleSpecified Then
                                tDBTV.Episodes.Add(mEpisode)
                            Else
                                logger.Warn(String.Format("Missing Episode Ignored | {0} - S{1}E{2} | No Episode Title found", mEpisode.TVShowDetails.Title, mEpisode.MainDetails.Season, mEpisode.MainDetails.Episode))
                            End If
                        End If
                    End If
                Else
                    logger.Warn("No valid episode or season number found")
                End If
            Next
        End If

        'create the "* All Seasons" entry if needed
        Dim tmpAllSeasons As Database.DBElement = tDBTV.Seasons.FirstOrDefault(Function(f) f.MainDetails.Season = 999)
        If tmpAllSeasons Is Nothing OrElse tmpAllSeasons.MainDetails Is Nothing Then
            tmpAllSeasons = New Database.DBElement(Enums.ContentType.TVSeason)
            tmpAllSeasons.MainDetails = New MediaContainers.MainDetails With {.Season = 999}
            tmpAllSeasons = Master.DB.AddTVShowInfoToDBElement(tmpAllSeasons, tDBTV)
            tDBTV.Seasons.Add(tmpAllSeasons)
        End If

        'cleanup seasons they don't have any episode
        Dim iIndex As Integer = 0
        While iIndex <= tDBTV.Seasons.Count - 1
            Dim iSeason As Integer = tDBTV.Seasons.Item(iIndex).MainDetails.Season
            If Not iSeason = 999 AndAlso tDBTV.Episodes.Where(Function(f) f.MainDetails.Season = iSeason).Count = 0 Then
                tDBTV.Seasons.RemoveAt(iIndex)
            Else
                iIndex += 1
            End If
        End While

        Return tDBTV
    End Function

    Public Shared Function MergeDataScraperResults_TVSeason(ByRef tDBElement As Database.DBElement, ByVal ScrapedList As List(Of MediaContainers.MainDetails)) As Database.DBElement

        'protects the first scraped result against overwriting
        Dim new_Aired As Boolean = False
        Dim new_Plot As Boolean = False
        Dim new_Season As Boolean = False
        Dim new_Title As Boolean = False

        For Each scrapedseason In ScrapedList

            'IDs
            If scrapedseason.TMDBSpecified Then
                tDBElement.MainDetails.TMDB = scrapedseason.TMDB
            End If
            If scrapedseason.TVDBSpecified Then
                tDBElement.MainDetails.TVDB = scrapedseason.TVDB
            End If

            'Season number
            If scrapedseason.SeasonSpecified AndAlso Not new_Season Then
                tDBElement.MainDetails.Season = scrapedseason.Season
                new_Season = True
            End If

            'Aired
            If (Not tDBElement.MainDetails.AiredSpecified OrElse Not Master.eSettings.TVLockEpisodeAired) AndAlso tDBElement.ScrapeOptions.bSeasonAired AndAlso
                scrapedseason.AiredSpecified AndAlso Master.eSettings.TVScraperEpisodeAired AndAlso Not new_Aired Then
                tDBElement.MainDetails.Aired = scrapedseason.Aired
                new_Aired = True
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperEpisodeAired AndAlso Not Master.eSettings.TVLockEpisodeAired Then
                tDBElement.MainDetails.Aired = String.Empty
            End If

            'Plot
            If (Not tDBElement.MainDetails.PlotSpecified OrElse Not Master.eSettings.TVLockEpisodePlot) AndAlso tDBElement.ScrapeOptions.bSeasonPlot AndAlso
                scrapedseason.PlotSpecified AndAlso Master.eSettings.TVScraperEpisodePlot AndAlso Not new_Plot Then
                tDBElement.MainDetails.Plot = scrapedseason.Plot
                new_Plot = True
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperEpisodePlot AndAlso Not Master.eSettings.TVLockEpisodePlot Then
                tDBElement.MainDetails.Plot = String.Empty
            End If

            'Title
            If (Not tDBElement.MainDetails.TitleSpecified OrElse Not Master.eSettings.TVLockSeasonTitle) AndAlso tDBElement.ScrapeOptions.bSeasonTitle AndAlso
                scrapedseason.TitleSpecified AndAlso Master.eSettings.TVScraperSeasonTitle AndAlso Not new_Title Then
                tDBElement.MainDetails.Title = scrapedseason.Title
                new_Title = True
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperSeasonTitle AndAlso Not Master.eSettings.TVLockSeasonTitle Then
                tDBElement.MainDetails.Title = String.Empty
            End If
        Next

        Return tDBElement
    End Function
    ''' <summary>
    ''' Returns the "merged" result of each data scraper results
    ''' </summary>
    ''' <param name="DBTVEpisode">Episode to be scraped</param>
    ''' <param name="ScrapedList"><c>List(Of MediaContainers.EpisodeDetails)</c> which contains unfiltered results of each data scraper</param>
    ''' <returns>The scrape result of episode (after applying various global scraper settings here)</returns>
    ''' <remarks>
    ''' This is used to determine the result of data scraping by going through all scraperesults of every data scraper and applying global data scraper settings here!
    ''' 
    ''' 2014/09/01 Cocotus - First implementation: Moved all global lock settings in various data scrapers to this function, only apply them once and not in every data scraper module! Should be more maintainable!
    ''' </remarks>
    Private Shared Function MergeDataScraperResults_TVEpisode(ByRef DBTVEpisode As Database.DBElement, ByVal ScrapedList As List(Of MediaContainers.MainDetails)) As Database.DBElement

        'protects the first scraped result against overwriting
        Dim new_Actors As Boolean = False
        Dim new_Aired As Boolean = False
        Dim new_Countries As Boolean = False
        Dim new_Credits As Boolean = False
        Dim new_Directors As Boolean = False
        Dim new_Episode As Boolean = False
        Dim new_GuestStars As Boolean = False
        Dim new_OriginalTitle As Boolean = False
        Dim new_Plot As Boolean = False
        Dim new_Rating As Boolean = False
        Dim new_Runtime As Boolean = False
        Dim new_Season As Boolean = False
        Dim new_ThumbPoster As Boolean = False
        Dim new_Title As Boolean = False
        Dim new_UserRating As Boolean = False

        ''If "Use Preview Datascraperresults" option is enabled, a preview window which displays all datascraperresults will be opened before showing the Edit Movie page!
        'If (ScrapeType = Enums.ScrapeType_Movie_MovieSet_TV.SingleScrape OrElse ScrapeType = Enums.ScrapeType_Movie_MovieSet_TV.SingleField) AndAlso Master.eSettings.MovieScraperUseDetailView AndAlso ScrapedList.Count > 0 Then
        '    PreviewDataScraperResults(ScrapedList)
        'End If

        For Each scrapedepisode In ScrapedList

            'IDs
            If scrapedepisode.IMDBSpecified Then
                DBTVEpisode.MainDetails.IMDB = scrapedepisode.IMDB
            End If
            If scrapedepisode.TMDBSpecified Then
                DBTVEpisode.MainDetails.TMDB = scrapedepisode.TMDB
            End If
            If scrapedepisode.TVDBSpecified Then
                DBTVEpisode.MainDetails.TVDB = scrapedepisode.TVDB
            End If

            'DisplayEpisode
            If scrapedepisode.DisplayEpisodeSpecified Then
                DBTVEpisode.MainDetails.DisplayEpisode = scrapedepisode.DisplayEpisode
            End If

            'DisplaySeason
            If scrapedepisode.DisplaySeasonSpecified Then
                DBTVEpisode.MainDetails.DisplaySeason = scrapedepisode.DisplaySeason
            End If

            'Actors
            If (Not DBTVEpisode.MainDetails.ActorsSpecified OrElse Not Master.eSettings.TVLockEpisodeActors) AndAlso DBTVEpisode.ScrapeOptions.bEpisodeActors AndAlso
                scrapedepisode.ActorsSpecified AndAlso Master.eSettings.TVScraperEpisodeActors AndAlso Not new_Actors Then

                'If Master.eSettings.TVScraperEpisodeCastWithImgOnly Then
                '    For i = scrapedepisode.Actors.Count - 1 To 0 Step -1
                '        If String.IsNullOrEmpty(scrapedepisode.Actors(i).ThumbURL) Then
                '            scrapedepisode.Actors.RemoveAt(i)
                '        End If
                '    Next
                'End If

                'If Master.eSettings.TVScraperEpisodeCastLimit > 0 AndAlso scrapedepisode.Actors.Count > Master.eSettings.TVScraperEpisodeCastLimit Then
                '    scrapedepisode.Actors.RemoveRange(Master.eSettings.TVScraperEpisodeCastLimit, scrapedepisode.Actors.Count - Master.eSettings.TVScraperEpisodeCastLimit)
                'End If

                DBTVEpisode.MainDetails.Actors = scrapedepisode.Actors
                'added check if there's any actors left to add, if not then try with results of following scraper...
                If scrapedepisode.ActorsSpecified Then
                    new_Actors = True
                    'add numbers for ordering
                    Dim iOrder As Integer = 0
                    For Each actor In scrapedepisode.Actors
                        actor.Order = iOrder
                        iOrder += 1
                    Next
                End If

            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperEpisodeActors AndAlso Not Master.eSettings.TVLockEpisodeActors Then
                DBTVEpisode.MainDetails.Actors.Clear()
            End If

            'Aired
            If (Not DBTVEpisode.MainDetails.AiredSpecified OrElse Not Master.eSettings.TVLockEpisodeAired) AndAlso DBTVEpisode.ScrapeOptions.bEpisodeAired AndAlso
                scrapedepisode.AiredSpecified AndAlso Master.eSettings.TVScraperEpisodeAired AndAlso Not new_Aired Then
                DBTVEpisode.MainDetails.Aired = NumUtils.DateToISO8601Date(scrapedepisode.Aired)
                new_Aired = True
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperEpisodeAired AndAlso Not Master.eSettings.TVLockEpisodeAired Then
                DBTVEpisode.MainDetails.Aired = String.Empty
            End If

            'Credits
            If (Not DBTVEpisode.MainDetails.CreditsSpecified OrElse Not Master.eSettings.TVLockEpisodeCredits) AndAlso
                scrapedepisode.CreditsSpecified AndAlso Master.eSettings.TVScraperEpisodeCredits AndAlso Not new_Credits Then
                DBTVEpisode.MainDetails.Credits = scrapedepisode.Credits
                new_Credits = True
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperEpisodeCredits AndAlso Not Master.eSettings.TVLockEpisodeCredits Then
                DBTVEpisode.MainDetails.Credits.Clear()
            End If

            'Directors
            If (Not DBTVEpisode.MainDetails.DirectorsSpecified OrElse Not Master.eSettings.TVLockEpisodeDirector) AndAlso DBTVEpisode.ScrapeOptions.bEpisodeDirectors AndAlso
                scrapedepisode.DirectorsSpecified AndAlso Master.eSettings.TVScraperEpisodeDirector AndAlso Not new_Directors Then
                DBTVEpisode.MainDetails.Directors = scrapedepisode.Directors
                new_Directors = True
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperEpisodeDirector AndAlso Not Master.eSettings.TVLockEpisodeDirector Then
                DBTVEpisode.MainDetails.Directors.Clear()
            End If

            'GuestStars
            If (Not DBTVEpisode.MainDetails.GuestStarsSpecified OrElse Not Master.eSettings.TVLockEpisodeGuestStars) AndAlso DBTVEpisode.ScrapeOptions.bEpisodeGuestStars AndAlso
                scrapedepisode.GuestStarsSpecified AndAlso Master.eSettings.TVScraperEpisodeGuestStars AndAlso Not new_GuestStars Then

                'If Master.eSettings.TVScraperEpisodeCastWithImgOnly Then
                '    For i = scrapedepisode.Actors.Count - 1 To 0 Step -1
                '        If String.IsNullOrEmpty(scrapedepisode.Actors(i).ThumbURL) Then
                '            scrapedepisode.Actors.RemoveAt(i)
                '        End If
                '    Next
                'End If

                'If Master.eSettings.TVScraperEpisodeCastLimit > 0 AndAlso scrapedepisode.Actors.Count > Master.eSettings.TVScraperEpisodeCastLimit Then
                '    scrapedepisode.Actors.RemoveRange(Master.eSettings.TVScraperEpisodeCastLimit, scrapedepisode.Actors.Count - Master.eSettings.TVScraperEpisodeCastLimit)
                'End If

                DBTVEpisode.MainDetails.GuestStars = scrapedepisode.GuestStars
                'added check if there's any actors left to add, if not then try with results of following scraper...
                If scrapedepisode.GuestStarsSpecified Then
                    new_GuestStars = True
                    'add numbers for ordering
                    Dim iOrder As Integer = 0
                    For Each aGuestStar In scrapedepisode.GuestStars
                        aGuestStar.Order = iOrder
                        iOrder += 1
                    Next
                End If

            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperEpisodeGuestStars AndAlso Not Master.eSettings.TVLockEpisodeGuestStars Then
                DBTVEpisode.MainDetails.GuestStars.Clear()
            End If

            'Plot
            If (Not DBTVEpisode.MainDetails.PlotSpecified OrElse Not Master.eSettings.TVLockEpisodePlot) AndAlso DBTVEpisode.ScrapeOptions.bEpisodePlot AndAlso
                scrapedepisode.PlotSpecified AndAlso Master.eSettings.TVScraperEpisodePlot AndAlso Not new_Plot Then
                DBTVEpisode.MainDetails.Plot = scrapedepisode.Plot
                new_Plot = True
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperEpisodePlot AndAlso Not Master.eSettings.TVLockEpisodePlot Then
                DBTVEpisode.MainDetails.Plot = String.Empty
            End If

            'Rating/Votes
            If (Not DBTVEpisode.MainDetails.RatingSpecified OrElse Not Master.eSettings.TVLockEpisodeRating) AndAlso DBTVEpisode.ScrapeOptions.bEpisodeRating AndAlso
                scrapedepisode.RatingSpecified AndAlso Master.eSettings.TVScraperEpisodeRating AndAlso Not new_Rating Then
                DBTVEpisode.MainDetails.Rating = scrapedepisode.Rating
                DBTVEpisode.MainDetails.Votes = NumUtils.CleanVotes(scrapedepisode.Votes)
                new_Rating = True
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperEpisodeRating AndAlso Not Master.eSettings.TVLockEpisodeRating Then
                DBTVEpisode.MainDetails.Rating = String.Empty
                DBTVEpisode.MainDetails.Votes = String.Empty
            End If

            'Runtime
            If (Not DBTVEpisode.MainDetails.RuntimeSpecified OrElse Not Master.eSettings.TVLockEpisodeRuntime) AndAlso DBTVEpisode.ScrapeOptions.bEpisodeRuntime AndAlso
                scrapedepisode.RuntimeSpecified AndAlso Master.eSettings.TVScraperEpisodeRuntime AndAlso Not new_Runtime Then
                DBTVEpisode.MainDetails.Runtime = scrapedepisode.Runtime
                new_Runtime = True
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperEpisodeRuntime AndAlso Not Master.eSettings.TVLockEpisodeRuntime Then
                DBTVEpisode.MainDetails.Runtime = String.Empty
            End If

            'ThumbPoster
            If (Not String.IsNullOrEmpty(scrapedepisode.ThumbPoster.URLOriginal) OrElse Not String.IsNullOrEmpty(scrapedepisode.ThumbPoster.URLThumb)) AndAlso Not new_ThumbPoster Then
                DBTVEpisode.MainDetails.ThumbPoster = scrapedepisode.ThumbPoster
                new_ThumbPoster = True
            End If

            'Title
            If (Not DBTVEpisode.MainDetails.TitleSpecified OrElse Not Master.eSettings.TVLockEpisodeTitle) AndAlso DBTVEpisode.ScrapeOptions.bEpisodeTitle AndAlso
               scrapedepisode.TitleSpecified AndAlso Master.eSettings.TVScraperEpisodeTitle AndAlso Not new_Title Then
                DBTVEpisode.MainDetails.Title = scrapedepisode.Title
                new_Title = True
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperEpisodeTitle AndAlso Not Master.eSettings.TVLockEpisodeTitle Then
                DBTVEpisode.MainDetails.Title = String.Empty
            End If

            'USerRating
            If (Not DBTVEpisode.MainDetails.UserRatingSpecified OrElse Not Master.eSettings.TVLockEpisodeUserRating) AndAlso DBTVEpisode.ScrapeOptions.bEpisodeUserRating AndAlso
                scrapedepisode.UserRatingSpecified AndAlso Master.eSettings.TVScraperEpisodeUserRating AndAlso Not new_UserRating Then
                DBTVEpisode.MainDetails.UserRating = scrapedepisode.UserRating
                new_UserRating = True
            ElseIf Master.eSettings.TVScraperCleanFields AndAlso Not Master.eSettings.TVScraperEpisodeUserRating AndAlso Not Master.eSettings.TVLockEpisodeUserRating Then
                DBTVEpisode.MainDetails.UserRating = 0
            End If
        Next

        'Add GuestStars to Actors
        If DBTVEpisode.MainDetails.GuestStarsSpecified AndAlso Master.eSettings.TVScraperEpisodeGuestStarsToActors AndAlso Not Master.eSettings.TVLockEpisodeActors Then
            DBTVEpisode.MainDetails.Actors.AddRange(DBTVEpisode.MainDetails.GuestStars)
        End If

        'TV Show Runtime for Episode Runtime
        If Not DBTVEpisode.MainDetails.RuntimeSpecified AndAlso Master.eSettings.TVScraperUseSRuntimeForEp AndAlso DBTVEpisode.TVShowDetails.RuntimeSpecified Then
            DBTVEpisode.MainDetails.Runtime = DBTVEpisode.TVShowDetails.Runtime
        End If

        Return DBTVEpisode
    End Function

    Public Shared Function MergeDataScraperResults_TVEpisode_Single(ByRef DBTVEpisode As Database.DBElement, ByVal ScrapedList As List(Of MediaContainers.MainDetails)) As Database.DBElement
        Dim KnownEpisodesIndex As New List(Of KnownEpisode)

        For Each kEpisode As MediaContainers.MainDetails In ScrapedList
            Dim nKnownEpisode As New KnownEpisode With {.AiredDate = kEpisode.Aired,
                                                        .Episode = kEpisode.Episode,
                                                        .EpisodeAbsolute = kEpisode.EpisodeAbsolute,
                                                        .EpisodeCombined = kEpisode.EpisodeCombined,
                                                        .EpisodeDVD = kEpisode.EpisodeDVD,
                                                        .Season = kEpisode.Season,
                                                        .SeasonCombined = kEpisode.SeasonCombined,
                                                        .SeasonDVD = kEpisode.SeasonDVD}
            If KnownEpisodesIndex.Where(Function(f) f.Episode = nKnownEpisode.Episode AndAlso f.Season = nKnownEpisode.Season).Count = 0 Then
                KnownEpisodesIndex.Add(nKnownEpisode)

                'try to get an episode information with more numbers
            ElseIf KnownEpisodesIndex.Where(Function(f) f.Episode = nKnownEpisode.Episode AndAlso f.Season = nKnownEpisode.Season AndAlso
                        ((nKnownEpisode.EpisodeAbsolute > -1 AndAlso Not f.EpisodeAbsolute = nKnownEpisode.EpisodeAbsolute) OrElse
                         (nKnownEpisode.EpisodeCombined > -1 AndAlso Not f.EpisodeCombined = nKnownEpisode.EpisodeCombined) OrElse
                         (nKnownEpisode.EpisodeDVD > -1 AndAlso Not f.EpisodeDVD = nKnownEpisode.EpisodeDVD) OrElse
                         (nKnownEpisode.SeasonCombined > -1 AndAlso Not f.SeasonCombined = nKnownEpisode.SeasonCombined) OrElse
                         (nKnownEpisode.SeasonDVD > -1 AndAlso Not f.SeasonDVD = nKnownEpisode.SeasonDVD))).Count = 1 Then
                Dim toRemove As KnownEpisode = KnownEpisodesIndex.FirstOrDefault(Function(f) f.Episode = nKnownEpisode.Episode AndAlso f.Season = nKnownEpisode.Season)
                KnownEpisodesIndex.Remove(toRemove)
                KnownEpisodesIndex.Add(nKnownEpisode)
            End If
        Next

        If KnownEpisodesIndex.Count = 1 Then
            'convert the episode and season number if needed
            Dim iEpisode As Integer = -1
            Dim iSeason As Integer = -1
            Dim strAiredDate As String = KnownEpisodesIndex.Item(0).AiredDate
            If DBTVEpisode.Ordering = Enums.EpisodeOrdering.Absolute Then
                iEpisode = KnownEpisodesIndex.Item(0).EpisodeAbsolute
                iSeason = 1
            ElseIf DBTVEpisode.Ordering = Enums.EpisodeOrdering.DVD Then
                iEpisode = CInt(KnownEpisodesIndex.Item(0).EpisodeDVD)
                iSeason = KnownEpisodesIndex.Item(0).SeasonDVD
            ElseIf DBTVEpisode.Ordering = Enums.EpisodeOrdering.Standard Then
                iEpisode = KnownEpisodesIndex.Item(0).Episode
                iSeason = KnownEpisodesIndex.Item(0).Season
            End If

            If Not iEpisode = -1 AndAlso Not iSeason = -1 Then
                MergeDataScraperResults_TVEpisode(DBTVEpisode, ScrapedList)
                If DBTVEpisode.MainDetails.Episode = -1 Then DBTVEpisode.MainDetails.Episode = iEpisode
                If DBTVEpisode.MainDetails.Season = -1 Then DBTVEpisode.MainDetails.Season = iSeason
            Else
                logger.Warn("No valid episode or season number found")
            End If
        Else
            logger.Warn("Episode could not be clearly determined.")
        End If

        Return DBTVEpisode
    End Function

    Public Shared Function CleanNFO_Movies(ByVal mNFO As MediaContainers.MainDetails) As MediaContainers.MainDetails
        If mNFO IsNot Nothing Then
            mNFO.Outline = mNFO.Outline.Replace(vbCrLf, vbLf).Replace(vbLf, vbCrLf)
            mNFO.Plot = mNFO.Plot.Replace(vbCrLf, vbLf).Replace(vbLf, vbCrLf)
            mNFO.ReleaseDate = NumUtils.DateToISO8601Date(mNFO.ReleaseDate)
            mNFO.Votes = NumUtils.CleanVotes(mNFO.Votes)
            If mNFO.FileInfoSpecified Then
                If mNFO.FileInfo.StreamDetails.AudioSpecified Then
                    For Each aStream In mNFO.FileInfo.StreamDetails.Audio.Where(Function(f) f.LanguageSpecified AndAlso Not f.LongLanguageSpecified)
                        aStream.LongLanguage = Localization.ISOGetLangByCode3(aStream.Language)
                    Next
                End If
                If mNFO.FileInfo.StreamDetails.SubtitleSpecified Then
                    For Each sStream In mNFO.FileInfo.StreamDetails.Subtitle.Where(Function(f) f.LanguageSpecified AndAlso Not f.LongLanguageSpecified)
                        sStream.LongLanguage = Localization.ISOGetLangByCode3(sStream.Language)
                    Next
                End If
            End If
            If mNFO.SetsSpecified Then
                For i = mNFO.Sets.Count - 1 To 0 Step -1
                    If Not mNFO.Sets(i).TitleSpecified Then
                        mNFO.Sets.RemoveAt(i)
                    End If
                Next
            End If

            'changes a LongLanguage to Alpha2 code
            If mNFO.LanguageSpecified Then
                Dim Language = APIXML.ScraperLanguagesXML.Languages.FirstOrDefault(Function(l) l.Name = mNFO.Language)
                If Language IsNot Nothing Then
                    mNFO.Language = Language.Abbreviation
                Else
                    'check if it's a valid Alpha2 code or remove the information the use the source default language
                    Dim ShortLanguage = APIXML.ScraperLanguagesXML.Languages.FirstOrDefault(Function(l) l.Abbreviation = mNFO.Language)
                    If ShortLanguage Is Nothing Then
                        mNFO.Language = String.Empty
                    End If
                End If
            End If

            Return mNFO
        Else
            Return mNFO
        End If
    End Function

    Public Shared Function CleanNFO_TVEpisodes(ByVal eNFO As MediaContainers.MainDetails) As MediaContainers.MainDetails
        If eNFO IsNot Nothing Then
            eNFO.Aired = NumUtils.DateToISO8601Date(eNFO.Aired)
            eNFO.Votes = NumUtils.CleanVotes(eNFO.Votes)
            If eNFO.FileInfoSpecified Then
                If eNFO.FileInfo.StreamDetails.AudioSpecified Then
                    For Each aStream In eNFO.FileInfo.StreamDetails.Audio.Where(Function(f) f.LanguageSpecified AndAlso Not f.LongLanguageSpecified)
                        aStream.LongLanguage = Localization.ISOGetLangByCode3(aStream.Language)
                    Next
                End If
                If eNFO.FileInfo.StreamDetails.SubtitleSpecified Then
                    For Each sStream In eNFO.FileInfo.StreamDetails.Subtitle.Where(Function(f) f.LanguageSpecified AndAlso Not f.LongLanguageSpecified)
                        sStream.LongLanguage = Localization.ISOGetLangByCode3(sStream.Language)
                    Next
                End If
            End If
            Return eNFO
        Else
            Return eNFO
        End If
    End Function

    Public Shared Function CleanNFO_TVShow(ByVal mNFO As MediaContainers.MainDetails) As MediaContainers.MainDetails
        If mNFO IsNot Nothing Then
            mNFO.Plot = mNFO.Plot.Replace(vbCrLf, vbLf).Replace(vbLf, vbCrLf)
            mNFO.Premiered = NumUtils.DateToISO8601Date(mNFO.Premiered)
            mNFO.Votes = NumUtils.CleanVotes(mNFO.Votes)

            'changes a LongLanguage to Alpha2 code
            If mNFO.LanguageSpecified Then
                Dim Language = APIXML.ScraperLanguagesXML.Languages.FirstOrDefault(Function(l) l.Name = mNFO.Language)
                If Language IsNot Nothing Then
                    mNFO.Language = Language.Abbreviation
                Else
                    'check if it's a valid Alpha2 code or remove the information the use the source default language
                    Dim ShortLanguage = APIXML.ScraperLanguagesXML.Languages.FirstOrDefault(Function(l) l.Abbreviation = mNFO.Language)
                    If ShortLanguage Is Nothing Then
                        mNFO.Language = String.Empty
                    End If
                End If
            End If

            'Boxee support
            If Master.eSettings.TVUseBoxee Then
                If mNFO.BoxeeTvDbSpecified AndAlso Not mNFO.TVDBSpecified Then
                    mNFO.TVDB = CInt(mNFO.BoxeeTvDb)
                    mNFO.BlankBoxeeId()
                End If
            End If

            Return mNFO
        Else
            Return mNFO
        End If
    End Function
    ''' <summary>
    ''' Delete all movie NFOs
    ''' </summary>
    ''' <param name="DBMovie"></param>
    ''' <remarks></remarks>
    Public Shared Sub DeleteNFO_Movie(ByVal DBMovie As Database.DBElement, ByVal ForceFileCleanup As Boolean)
        If Not DBMovie.FilenameSpecified Then Return

        Try
            For Each a In FileUtils.GetFilenameList.Movie(DBMovie, Enums.ScrapeModifierType.MainNFO, ForceFileCleanup)
                If File.Exists(a) Then
                    File.Delete(a)
                End If
            Next
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name & Convert.ToChar(Windows.Forms.Keys.Tab) & "<" & DBMovie.Filename & ">")
        End Try
    End Sub
    ''' <summary>
    ''' Delete all movie NFOs
    ''' </summary>
    ''' <param name="DBMovieSet"></param>
    ''' <remarks></remarks>
    Public Shared Sub DeleteNFO_MovieSet(ByVal DBMovieSet As Database.DBElement, ByVal ForceFileCleanup As Boolean, Optional bForceOldTitle As Boolean = False)
        If Not DBMovieSet.MainDetails.TitleSpecified Then Return

        Try
            For Each a In FileUtils.GetFilenameList.MovieSet(DBMovieSet, Enums.ScrapeModifierType.MainNFO, bForceOldTitle)
                If File.Exists(a) Then
                    File.Delete(a)
                End If
            Next
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name & Convert.ToChar(Windows.Forms.Keys.Tab) & "<" & DBMovieSet.Filename & ">")
        End Try
    End Sub

    Public Shared Function FIToString(ByVal miFI As MediaContainers.Fileinfo, ByVal isTV As Boolean) As String
        '//
        ' Convert Fileinfo into a string to be displayed in the GUI
        '\\

        Dim strOutput As New StringBuilder
        Dim iVS As Integer = 1
        Dim iAS As Integer = 1
        Dim iSS As Integer = 1

        Try
            If miFI IsNot Nothing Then

                If miFI.StreamDetails IsNot Nothing Then
                    If miFI.StreamDetails.VideoSpecified Then strOutput.AppendFormat("{0}: {1}{2}", Master.eLang.GetString(595, "Video Streams"), miFI.StreamDetails.Video.Count.ToString, Environment.NewLine)
                    If miFI.StreamDetails.AudioSpecified Then strOutput.AppendFormat("{0}: {1}{2}", Master.eLang.GetString(596, "Audio Streams"), miFI.StreamDetails.Audio.Count.ToString, Environment.NewLine)
                    If miFI.StreamDetails.SubtitleSpecified Then strOutput.AppendFormat("{0}: {1}{2}", Master.eLang.GetString(597, "Subtitle  Streams"), miFI.StreamDetails.Subtitle.Count.ToString, Environment.NewLine)
                    For Each miVideo As MediaContainers.Video In miFI.StreamDetails.Video
                        strOutput.AppendFormat("{0}{1} {2}{0}", Environment.NewLine, Master.eLang.GetString(617, "Video Stream"), iVS)
                        If miVideo.WidthSpecified AndAlso miVideo.HeightSpecified Then strOutput.AppendFormat("- {0}{1}", String.Format(Master.eLang.GetString(269, "Size: {0}x{1}"), miVideo.Width, miVideo.Height), Environment.NewLine)
                        If miVideo.AspectSpecified Then strOutput.AppendFormat("- {0}: {1}{2}", Master.eLang.GetString(614, "Aspect Ratio"), miVideo.Aspect, Environment.NewLine)
                        If miVideo.ScantypeSpecified Then strOutput.AppendFormat("- {0}: {1}{2}", Master.eLang.GetString(605, "Scan Type"), miVideo.Scantype, Environment.NewLine)
                        If miVideo.CodecSpecified Then strOutput.AppendFormat("- {0}: {1}{2}", Master.eLang.GetString(604, "Codec"), miVideo.Codec, Environment.NewLine)
                        If miVideo.BitrateSpecified Then strOutput.AppendFormat("- {0}: {1}{2}", "Bitrate", miVideo.Bitrate, Environment.NewLine)
                        If miVideo.DurationSpecified Then strOutput.AppendFormat("- {0}: {1}{2}", Master.eLang.GetString(609, "Duration"), miVideo.Duration, Environment.NewLine)
                        'for now return filesize in mbytes instead of bytes(default)
                        If miVideo.Filesize > 0 Then strOutput.AppendFormat("- {0}: {1}{2}", Master.eLang.GetString(1455, "Filesize [MB]"), CStr(NumUtils.ConvertBytesTo(CLng(miVideo.Filesize), NumUtils.FileSizeUnit.Megabyte, 0)), Environment.NewLine)
                        If miVideo.LongLanguageSpecified Then strOutput.AppendFormat("- {0}: {1}{2}", Master.eLang.GetString(610, "Language"), miVideo.LongLanguage, Environment.NewLine)
                        If miVideo.MultiViewCountSpecified Then strOutput.AppendFormat("- {0}: {1}{2}", Master.eLang.GetString(1156, "MultiView Count"), miVideo.MultiViewCount, Environment.NewLine)
                        If miVideo.MultiViewLayoutSpecified Then strOutput.AppendFormat("- {0}: {1}{2}", Master.eLang.GetString(1157, "MultiView Layout"), miVideo.MultiViewLayout, Environment.NewLine)
                        If miVideo.StereoModeSpecified Then strOutput.AppendFormat("- {0}: {1} ({2})", Master.eLang.GetString(1286, "StereoMode"), miVideo.StereoMode, miVideo.ShortStereoMode)
                        iVS += 1
                    Next

                    strOutput.Append(Environment.NewLine)

                    For Each miAudio As MediaContainers.Audio In miFI.StreamDetails.Audio
                        'audio
                        strOutput.AppendFormat("{0}{1} {2}{0}", Environment.NewLine, Master.eLang.GetString(618, "Audio Stream"), iAS.ToString)
                        If miAudio.CodecSpecified Then strOutput.AppendFormat("- {0}: {1}{2}", Master.eLang.GetString(604, "Codec"), miAudio.Codec, Environment.NewLine)
                        If miAudio.ChannelsSpecified Then strOutput.AppendFormat("- {0}: {1}{2}", Master.eLang.GetString(611, "Channels"), miAudio.Channels, Environment.NewLine)
                        If miAudio.BitrateSpecified Then strOutput.AppendFormat("- {0}: {1}{2}", "Bitrate", miAudio.Bitrate, Environment.NewLine)
                        If miAudio.LongLanguageSpecified Then strOutput.AppendFormat("- {0}: {1}", Master.eLang.GetString(610, "Language"), miAudio.LongLanguage)
                        iAS += 1
                    Next

                    strOutput.Append(Environment.NewLine)

                    For Each miSub As MediaContainers.Subtitle In miFI.StreamDetails.Subtitle
                        'subtitles
                        strOutput.AppendFormat("{0}{1} {2}{0}", Environment.NewLine, Master.eLang.GetString(619, "Subtitle Stream"), iSS.ToString)
                        If miSub.LongLanguageSpecified Then strOutput.AppendFormat("- {0}: {1}", Master.eLang.GetString(610, "Language"), miSub.LongLanguage)
                        iSS += 1
                    Next
                End If
            End If
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try

        If strOutput.ToString.Trim.Length > 0 Then
            Return strOutput.ToString
        Else
            If isTV Then
                Return Master.eLang.GetString(504, "Meta Data is not available for this episode. Try rescanning.")
            Else
                Return Master.eLang.GetString(419, "Meta Data is not available for this movie. Try rescanning.")
            End If
        End If
    End Function

    ''' <summary>
    ''' Return the "best" or the "prefered language" audio stream of the videofile
    ''' </summary>
    ''' <param name="miFIA"><c>MediaInfo.Fileinfo</c> The Mediafile-container of the videofile</param>
    ''' <returns>The best <c>MediaInfo.Audio</c> stream information of the videofile</returns>
    ''' <remarks>
    ''' This is used to determine which audio stream information should be displayed in Ember main view (icon display)
    ''' The audiostream with most channels will be returned - if there are 2 or more streams which have the same "highest" channelcount then either the "DTSHD" stream or the one with highest bitrate will be returned
    ''' 
    ''' 2014/08/12 cocotus - Should work better: If there's more than one audiostream which highest channelcount, the one with highest bitrate or the DTSHD stream will be returned
    ''' </remarks>
    Public Shared Function GetBestAudio(ByVal miFIA As MediaContainers.Fileinfo, ByVal ForTV As Boolean) As MediaContainers.Audio
        '//
        ' Get the highest values from file info
        '\\

        Dim fiaOut As New MediaContainers.Audio
        Try
            Dim cmiFIA As New MediaContainers.Fileinfo

            Dim getPrefLanguage As Boolean = False
            Dim hasPrefLanguage As Boolean = False
            Dim prefLanguage As String = String.Empty
            Dim sinMostChannels As Single = 0
            Dim sinChans As Single = 0
            Dim sinMostBitrate As Single = 0
            Dim sinBitrate As Single = 0
            Dim sinCodec As String = String.Empty
            fiaOut.Codec = String.Empty
            fiaOut.Channels = String.Empty
            fiaOut.Language = String.Empty
            fiaOut.LongLanguage = String.Empty
            fiaOut.Bitrate = String.Empty

            If ForTV Then
                If Not String.IsNullOrEmpty(Master.eSettings.TVGeneralFlagLang) Then
                    getPrefLanguage = True
                    prefLanguage = Master.eSettings.TVGeneralFlagLang.ToLower
                End If
            Else
                If Not String.IsNullOrEmpty(Master.eSettings.MovieGeneralFlagLang) Then
                    getPrefLanguage = True
                    prefLanguage = Master.eSettings.MovieGeneralFlagLang.ToLower
                End If
            End If

            If getPrefLanguage AndAlso miFIA.StreamDetails.Audio.Where(Function(f) f.LongLanguage.ToLower = prefLanguage).Count > 0 Then
                For Each Stream As MediaContainers.Audio In miFIA.StreamDetails.Audio
                    If Stream.LongLanguage.ToLower = prefLanguage Then
                        cmiFIA.StreamDetails.Audio.Add(Stream)
                    End If
                Next
            Else
                cmiFIA.StreamDetails.Audio.AddRange(miFIA.StreamDetails.Audio)
            End If

            For Each miAudio As MediaContainers.Audio In cmiFIA.StreamDetails.Audio
                If Not String.IsNullOrEmpty(miAudio.Channels) Then
                    sinChans = NumUtils.ConvertToSingle(MediaInfo.FormatAudioChannel(miAudio.Channels))
                    sinBitrate = 0
                    If Integer.TryParse(miAudio.Bitrate, 0) Then
                        sinBitrate = CInt(miAudio.Bitrate)
                    End If
                    If sinChans >= sinMostChannels AndAlso (sinBitrate > sinMostBitrate OrElse miAudio.Codec.ToLower.Contains("dtshd") OrElse sinBitrate = 0) Then
                        If Integer.TryParse(miAudio.Bitrate, 0) Then
                            sinMostBitrate = CInt(miAudio.Bitrate)
                        End If
                        sinMostChannels = sinChans
                        fiaOut.Bitrate = miAudio.Bitrate
                        fiaOut.Channels = sinChans.ToString
                        fiaOut.Codec = miAudio.Codec
                        fiaOut.Language = miAudio.Language
                        fiaOut.LongLanguage = miAudio.LongLanguage
                    End If
                End If

                If ForTV Then
                    If Not String.IsNullOrEmpty(Master.eSettings.TVGeneralFlagLang) AndAlso miAudio.LongLanguage.ToLower = Master.eSettings.TVGeneralFlagLang.ToLower Then fiaOut.HasPreferred = True
                Else
                    If Not String.IsNullOrEmpty(Master.eSettings.MovieGeneralFlagLang) AndAlso miAudio.LongLanguage.ToLower = Master.eSettings.MovieGeneralFlagLang.ToLower Then fiaOut.HasPreferred = True
                End If
            Next

        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
        Return fiaOut
    End Function

    Public Shared Function GetBestVideo(ByVal miFIV As MediaContainers.Fileinfo) As MediaContainers.Video
        '//
        ' Get the highest values from file info
        '\\

        Dim fivOut As New MediaContainers.Video
        Try
            Dim iWidest As Integer = 0
            Dim iWidth As Integer = 0

            'set some defaults to make it easy on ourselves
            fivOut.Width = String.Empty
            fivOut.Height = String.Empty
            fivOut.Aspect = String.Empty
            fivOut.Codec = String.Empty
            fivOut.Duration = String.Empty
            fivOut.Scantype = String.Empty
            fivOut.Language = String.Empty
            'cocotus, 2013/02 Added support for new MediaInfo-fields
            fivOut.Bitrate = String.Empty
            fivOut.MultiViewCount = String.Empty
            fivOut.MultiViewLayout = String.Empty
            fivOut.Filesize = 0
            'cocotus end

            For Each miVideo As MediaContainers.Video In miFIV.StreamDetails.Video
                If Not String.IsNullOrEmpty(miVideo.Width) Then
                    If Integer.TryParse(miVideo.Width, 0) Then
                        iWidth = Convert.ToInt32(miVideo.Width)
                    Else
                        logger.Warn("[GetBestVideo] Invalid width(not a number!) of videostream: " & miVideo.Width)
                    End If
                    If iWidth > iWidest Then
                        iWidest = iWidth
                        fivOut.Width = miVideo.Width
                        fivOut.Height = miVideo.Height
                        fivOut.Aspect = miVideo.Aspect
                        fivOut.Codec = miVideo.Codec
                        fivOut.Duration = miVideo.Duration
                        fivOut.Scantype = miVideo.Scantype
                        fivOut.Language = miVideo.Language

                        'cocotus, 2013/02 Added support for new MediaInfo-fields

                        'MultiViewCount (3D) handling, simply map field
                        fivOut.MultiViewCount = miVideo.MultiViewCount

                        'MultiViewLayout (3D) handling, simply map field
                        fivOut.MultiViewLayout = miVideo.MultiViewLayout

                        'FileSize handling, simply map field
                        fivOut.Filesize = miVideo.Filesize

                        'Bitrate handling, simply map field
                        fivOut.Bitrate = miVideo.Bitrate
                        'cocotus end

                    End If
                End If
            Next

        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
        Return fivOut
    End Function

    Public Shared Function GetDimensionsFromVideo(ByVal fiRes As MediaContainers.Video) As String
        '//
        ' Get the dimension values of the video from the information provided by MediaInfo.dll
        '\\

        Dim result As String = String.Empty
        Try
            If Not String.IsNullOrEmpty(fiRes.Width) AndAlso Not String.IsNullOrEmpty(fiRes.Height) AndAlso Not String.IsNullOrEmpty(fiRes.Aspect) Then
                Dim iWidth As Integer = Convert.ToInt32(fiRes.Width)
                Dim iHeight As Integer = Convert.ToInt32(fiRes.Height)
                Dim sinADR As Single = NumUtils.ConvertToSingle(fiRes.Aspect)

                result = String.Format("{0}x{1} ({2})", iWidth, iHeight, sinADR.ToString("0.00"))
            End If
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try

        Return result
    End Function

    Public Shared Function GetIMDBFromNonConf(ByVal sPath As String, ByVal isSingle As Boolean) As NonConf
        Dim tNonConf As New NonConf
        Dim dirPath As String = Directory.GetParent(sPath).FullName
        Dim lFiles As New List(Of String)

        If isSingle Then
            Try
                lFiles.AddRange(Directory.GetFiles(dirPath, "*.nfo"))
            Catch
            End Try
            Try
                lFiles.AddRange(Directory.GetFiles(dirPath, "*.info"))
            Catch
            End Try
        Else
            Dim fName As String = Path.GetFileNameWithoutExtension(FileUtils.Common.RemoveStackingMarkers(sPath)).ToLower
            Dim oName As String = Path.GetFileNameWithoutExtension(sPath)
            fName = If(fName.EndsWith("*"), fName, String.Concat(fName, "*"))
            oName = If(oName.EndsWith("*"), oName, String.Concat(oName, "*"))

            Try
                lFiles.AddRange(Directory.GetFiles(dirPath, String.Concat(fName, ".nfo")))
            Catch
            End Try
            Try
                lFiles.AddRange(Directory.GetFiles(dirPath, String.Concat(oName, ".nfo")))
            Catch
            End Try
            Try
                lFiles.AddRange(Directory.GetFiles(dirPath, String.Concat(fName, ".info")))
            Catch
            End Try
            Try
                lFiles.AddRange(Directory.GetFiles(dirPath, String.Concat(oName, ".info")))
            Catch
            End Try
        End If

        For Each sFile As String In lFiles
            Using srInfo As New StreamReader(sFile)
                Dim sInfo As String = srInfo.ReadToEnd
                Dim sIMDBID As String = Regex.Match(sInfo, "tt\d\d\d\d\d\d\d*", RegexOptions.Multiline Or RegexOptions.Singleline Or RegexOptions.IgnoreCase).ToString

                If Not String.IsNullOrEmpty(sIMDBID) Then
                    tNonConf.IMDBID = sIMDBID
                    'now lets try to see if the rest of the file is a proper nfo
                    If sInfo.ToLower.Contains("</movie>") Then
                        tNonConf.Text = APIXML.XMLToLowerCase(sInfo.Substring(0, sInfo.ToLower.IndexOf("</movie>") + 8))
                    End If
                    Exit For
                Else
                    sIMDBID = Regex.Match(sPath, "tt\d\d\d\d\d\d\d*", RegexOptions.Multiline Or RegexOptions.Singleline Or RegexOptions.IgnoreCase).ToString
                    If Not String.IsNullOrEmpty(sIMDBID) Then
                        tNonConf.IMDBID = sIMDBID
                    End If
                End If
            End Using
        Next
        Return tNonConf
    End Function

    Public Shared Function GetNfoPath_MovieSet(ByVal DBElement As Database.DBElement) As String
        For Each a In FileUtils.GetFilenameList.MovieSet(DBElement, Enums.ScrapeModifierType.MainNFO)
            If File.Exists(a) Then
                Return a
            End If
        Next

        Return String.Empty
    End Function
    ''' <summary>
    ''' Get the resolution of the video from the dimensions provided by MediaInfo.dll
    ''' </summary>
    ''' <param name="fiRes"></param>
    ''' <returns></returns>
    Public Shared Function GetResFromDimensions(ByVal fiRes As MediaContainers.Video) As String
        Dim resOut As String = String.Empty
        Try
            If Not String.IsNullOrEmpty(fiRes.Width) AndAlso Not String.IsNullOrEmpty(fiRes.Height) AndAlso Not String.IsNullOrEmpty(fiRes.Aspect) Then
                Dim iWidth As Integer = Convert.ToInt32(fiRes.Width)
                Dim iHeight As Integer = Convert.ToInt32(fiRes.Height)
                Dim sinADR As Single = NumUtils.ConvertToSingle(fiRes.Aspect)

                Select Case True
                    Case iWidth < 640
                        resOut = "SD"
                    'exact
                    Case (iWidth = 3840 AndAlso iHeight = 2160) OrElse (iWidth = 3996 AndAlso iHeight = 2160) OrElse (iWidth = 4096 AndAlso iHeight = 2160) OrElse (iWidth = 5120 AndAlso iHeight = 2160)
                        resOut = "2160"
                    Case (iWidth = 2560 AndAlso iHeight = 1440)
                        resOut = "1440"
                    Case (iWidth = 1920 AndAlso (iHeight = 1080 OrElse iHeight = 800)) OrElse (iWidth = 1440 AndAlso iHeight = 1080) OrElse (iWidth = 1280 AndAlso iHeight = 1080)
                        resOut = "1080"
                    Case (iWidth = 1366 AndAlso iHeight = 768) OrElse (iWidth = 1024 AndAlso iHeight = 768)
                        resOut = "768"
                    Case (iWidth = 960 AndAlso iHeight = 720) OrElse (iWidth = 1280 AndAlso (iHeight = 720 OrElse iHeight = 544))
                        resOut = "720"
                    Case (iWidth = 1024 AndAlso iHeight = 576) OrElse (iWidth = 720 AndAlso iHeight = 576)
                        resOut = "576"
                    Case (iWidth = 720 OrElse iWidth = 960) AndAlso iHeight = 540
                        resOut = "540"
                    Case (iWidth = 852 OrElse iWidth = 720 OrElse iWidth = 704 OrElse iWidth = 640) AndAlso iHeight = 480
                        resOut = "480"
                    'by ADR
                    Case sinADR >= 1.4 AndAlso iWidth = 3840
                        resOut = "2160"
                    Case sinADR >= 1.4 AndAlso iWidth = 2560
                        resOut = "1440"
                    Case sinADR >= 1.4 AndAlso iWidth = 1920
                        resOut = "1080"
                    Case sinADR >= 1.4 AndAlso iWidth = 1366
                        resOut = "768"
                    Case sinADR >= 1.4 AndAlso iWidth = 1280
                        resOut = "720"
                    Case sinADR >= 1.4 AndAlso iWidth = 1024
                        resOut = "576"
                    Case sinADR >= 1.4 AndAlso iWidth = 960
                        resOut = "540"
                    Case sinADR >= 1.4 AndAlso iWidth = 852
                        resOut = "480"
                    'loose
                    Case iWidth > 2560 AndAlso iHeight > 1440
                        resOut = "2160"
                    Case iWidth > 1920 AndAlso iHeight > 1080
                        resOut = "1440"
                    Case iWidth >= 1200 AndAlso iHeight > 768
                        resOut = "1080"
                    Case iWidth >= 1000 AndAlso iHeight > 720
                        resOut = "768"
                    Case iWidth >= 1000 AndAlso iHeight > 500
                        resOut = "720"
                    Case iWidth >= 700 AndAlso iHeight > 540
                        resOut = "576"
                    Case iWidth >= 700 AndAlso iHeight > 480
                        resOut = "540"
                    Case Else
                        resOut = "480"
                End Select
            End If
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try

        If Not String.IsNullOrEmpty(resOut) Then
            If String.IsNullOrEmpty(fiRes.Scantype) Then
                Return String.Concat(resOut)
            Else
                Return String.Concat(resOut, If(fiRes.Scantype.ToLower = "progressive", "p", "i"))
            End If
        Else
            Return String.Empty
        End If
    End Function

    Public Shared Function IsConformingNFO_Movie(ByVal sPath As String) As Boolean
        Dim xmlRootAtt As New XmlRootAttribute With {.ElementName = "movie"}
        Dim xmlSer As XmlSerializer = New XmlSerializer(GetType(MediaContainers.MainDetails), xmlRootAtt)

        Try
            If File.Exists(sPath) Then
                Using testSR As StreamReader = New StreamReader(sPath)
                    Dim testMovie As MediaContainers.MainDetails = DirectCast(xmlSer.Deserialize(testSR), MediaContainers.MainDetails)
                End Using
                Return True
            Else
                Return False
            End If
        Catch
            Return False
        End Try
    End Function

    Public Shared Function IsConformingNFO_TVEpisode(ByVal sPath As String) As Boolean
        Dim xmlRootAtt As New XmlRootAttribute With {.ElementName = "episodedetails"}
        Dim xmlSer As XmlSerializer = New XmlSerializer(GetType(MediaContainers.MainDetails), xmlRootAtt)
        Dim nTVEpisode As New MediaContainers.MainDetails

        Try
            If File.Exists(sPath) Then
                Using xmlSR As StreamReader = New StreamReader(sPath)
                    Dim xmlStr As String = xmlSR.ReadToEnd
                    Dim rMatches As MatchCollection = Regex.Matches(xmlStr, "<episodedetails.*?>.*?</episodedetails>", RegexOptions.IgnoreCase Or RegexOptions.Singleline Or RegexOptions.IgnorePatternWhitespace)
                    If rMatches.Count = 1 Then
                        Using xmlRead As StringReader = New StringReader(rMatches(0).Value)
                            nTVEpisode = DirectCast(xmlSer.Deserialize(xmlRead), MediaContainers.MainDetails)
                        End Using
                        Return True
                    ElseIf rMatches.Count > 1 Then
                        'read them all... if one fails, the entire nfo is non conforming
                        For Each xmlReg As Match In rMatches
                            Using xmlRead As StringReader = New StringReader(xmlReg.Value)
                                nTVEpisode = DirectCast(xmlSer.Deserialize(xmlRead), MediaContainers.MainDetails)
                                nTVEpisode = Nothing
                            End Using
                        Next
                        Return True
                    Else
                        Return False
                    End If
                End Using
            Else
                Return False
            End If
        Catch
            Return False
        End Try
    End Function

    Public Shared Function IsConformingNFO_TVShow(ByVal sPath As String) As Boolean
        Dim xmlRootAtt As New XmlRootAttribute With {.ElementName = "tvshow"}
        Dim xmlSer As XmlSerializer = New XmlSerializer(GetType(MediaContainers.MainDetails), xmlRootAtt)

        Try
            If File.Exists(sPath) Then
                Using testSR As StreamReader = New StreamReader(sPath)
                    Dim testShow As MediaContainers.MainDetails = DirectCast(xmlSer.Deserialize(testSR), MediaContainers.MainDetails)
                End Using
                Return True
            Else
                Return False
            End If
        Catch
            Return False
        End Try
    End Function

    Public Shared Function LoadFromNFO_Movie(ByVal sPath As String, ByVal isSingle As Boolean) As MediaContainers.MainDetails
        Dim xmlRootAtt As New XmlRootAttribute With {.ElementName = "movie"}
        Dim xmlSer As XmlSerializer = New XmlSerializer(GetType(MediaContainers.MainDetails), xmlRootAtt)
        Dim nMovie As New MediaContainers.MainDetails

        If Not String.IsNullOrEmpty(sPath) Then
            Try
                If File.Exists(sPath) Then
                    Using xmlSR As StreamReader = New StreamReader(sPath)
                        nMovie = DirectCast(xmlSer.Deserialize(xmlSR), MediaContainers.MainDetails)
                        nMovie = CleanNFO_Movies(nMovie)
                    End Using
                Else
                    If Not String.IsNullOrEmpty(sPath) Then
                        Dim sReturn As New NonConf
                        sReturn = GetIMDBFromNonConf(sPath, isSingle)
                        nMovie.IMDB = sReturn.IMDBID
                        Try
                            If Not String.IsNullOrEmpty(sReturn.Text) Then
                                Using xmlSTR As StringReader = New StringReader(sReturn.Text)
                                    xmlSer = New XmlSerializer(GetType(MediaContainers.MainDetails), xmlRootAtt)
                                    nMovie = DirectCast(xmlSer.Deserialize(xmlSTR), MediaContainers.MainDetails)
                                    nMovie.IMDB = sReturn.IMDBID
                                    nMovie = CleanNFO_Movies(nMovie)
                                End Using
                            End If
                        Catch
                        End Try
                    End If
                End If

            Catch ex As Exception
                logger.Error(ex, New StackFrame().GetMethod().Name)

                nMovie.Clear()
                If Not String.IsNullOrEmpty(sPath) Then

                    'go ahead and rename it now, will still be picked up in getimdbfromnonconf
                    If Not Master.eSettings.GeneralOverwriteNfo Then
                        RenameNonConfNFO_Movie(sPath, True)
                    End If

                    Dim sReturn As New NonConf
                    sReturn = GetIMDBFromNonConf(sPath, isSingle)
                    nMovie.IMDB = sReturn.IMDBID
                    Try
                        If Not String.IsNullOrEmpty(sReturn.Text) Then
                            Using xmlSTR As StringReader = New StringReader(sReturn.Text)
                                xmlSer = New XmlSerializer(GetType(MediaContainers.MainDetails), xmlRootAtt)
                                nMovie = DirectCast(xmlSer.Deserialize(xmlSTR), MediaContainers.MainDetails)
                                nMovie.IMDB = sReturn.IMDBID
                                nMovie = CleanNFO_Movies(nMovie)
                            End Using
                        End If
                    Catch
                    End Try
                End If
            End Try

            If xmlSer IsNot Nothing Then
                xmlSer = Nothing
            End If
        End If

        Return nMovie
    End Function

    Public Shared Function LoadFromNFO_MovieSet(ByVal sPath As String) As MediaContainers.MainDetails
        Dim xmlRootAtt As New XmlRootAttribute With {.ElementName = "movieset"}
        Dim xmlSer As XmlSerializer = New XmlSerializer(GetType(MediaContainers.MainDetails), xmlRootAtt)
        Dim nMovieSet As New MediaContainers.MainDetails

        If Not String.IsNullOrEmpty(sPath) Then
            Try
                If File.Exists(sPath) Then
                    Using xmlSR As StreamReader = New StreamReader(sPath)
                        nMovieSet = DirectCast(xmlSer.Deserialize(xmlSR), MediaContainers.MainDetails)
                        nMovieSet.Plot = nMovieSet.Plot.Replace(vbCrLf, vbLf).Replace(vbLf, vbCrLf)
                    End Using
                    'Else
                    '    If Not String.IsNullOrEmpty(sPath) Then
                    '        Dim sReturn As New NonConf
                    '        sReturn = GetIMDBFromNonConf(sPath, isSingle)
                    '        xmlMov.IMDBID = sReturn.IMDBID
                    '        Try
                    '            If Not String.IsNullOrEmpty(sReturn.Text) Then
                    '                Using xmlSTR As StringReader = New StringReader(sReturn.Text)
                    '                    xmlSer = New XmlSerializer(GetType(MediaContainers.MainDetails), xmlRootAtt)
                    '                    xmlMov = DirectCast(xmlSer.Deserialize(xmlSTR), MediaContainers.Movie)
                    '                    xmlMov.Genre = Strings.Join(xmlMov.LGenre.ToArray, " / ")
                    '                    xmlMov.Outline = xmlMov.Outline.Replace(vbCrLf, vbLf).Replace(vbLf, vbCrLf)
                    '                    xmlMovSet.Plot = xmlMovSet.Plot.Replace(vbCrLf, vbLf).Replace(vbLf, vbCrLf)
                    '                    xmlMov.IMDBID = sReturn.IMDBID
                    '                End Using
                    '            End If
                    '        Catch
                    '        End Try
                    '    End If
                End If

            Catch ex As Exception
                logger.Error(ex, New StackFrame().GetMethod().Name)

                nMovieSet.Clear()
                'If Not String.IsNullOrEmpty(sPath) Then

                '    'go ahead and rename it now, will still be picked up in getimdbfromnonconf
                '    If Not Master.eSettings.GeneralOverwriteNfo Then
                '        RenameNonConfNfo(sPath, True)
                '    End If

                '    Dim sReturn As New NonConf
                '    sReturn = GetIMDBFromNonConf(sPath, isSingle)
                '    xmlMov.IMDBID = sReturn.IMDBID
                '    Try
                '        If Not String.IsNullOrEmpty(sReturn.Text) Then
                '            Using xmlSTR As StringReader = New StringReader(sReturn.Text)
                '                xmlSer = New XmlSerializer(GetType(MediaContainers.MainDetails), xmlRootAtt)
                '                xmlMov = DirectCast(xmlSer.Deserialize(xmlSTR), MediaContainers.Movie)
                '                xmlMov.Genre = Strings.Join(xmlMov.LGenre.ToArray, " / ")
                '                xmlMov.Outline = xmlMov.Outline.Replace(vbCrLf, vbLf).Replace(vbLf, vbCrLf)
                '                xmlMovSet.Plot = xmlMovSet.Plot.Replace(vbCrLf, vbLf).Replace(vbLf, vbCrLf)
                '                xmlMov.IMDBID = sReturn.IMDBID
                '            End Using
                '        End If
                '    Catch
                '    End Try
                'End If
            End Try

            If xmlSer IsNot Nothing Then
                xmlSer = Nothing
            End If
        End If

        Return nMovieSet
    End Function

    Public Shared Function LoadFromNFO_TVEpisode(ByVal sPath As String, ByVal SeasonNumber As Integer, ByVal EpisodeNumber As Integer) As MediaContainers.MainDetails
        Dim xmlRootAtt As New XmlRootAttribute With {.ElementName = "episodedetails"}
        Dim xmlSer As XmlSerializer = New XmlSerializer(GetType(MediaContainers.MainDetails), xmlRootAtt)
        Dim nTVEpisode As New MediaContainers.MainDetails

        If Not String.IsNullOrEmpty(sPath) AndAlso SeasonNumber >= -1 Then
            Try
                If File.Exists(sPath) Then
                    'better way to read multi-root xml??
                    Using xmlSR As StreamReader = New StreamReader(sPath)
                        Dim xmlStr As String = xmlSR.ReadToEnd
                        Dim rMatches As MatchCollection = Regex.Matches(xmlStr, "<episodedetails.*?>.*?</episodedetails>", RegexOptions.IgnoreCase Or RegexOptions.Singleline Or RegexOptions.IgnorePatternWhitespace)
                        If rMatches.Count = 1 Then
                            'only one episodedetail... assume it's the proper one
                            Using xmlRead As StringReader = New StringReader(rMatches(0).Value)
                                nTVEpisode = DirectCast(xmlSer.Deserialize(xmlRead), MediaContainers.MainDetails)
                                nTVEpisode = CleanNFO_TVEpisodes(nTVEpisode)
                                xmlSer = Nothing
                                If nTVEpisode.FileInfoSpecified Then
                                    If nTVEpisode.FileInfo.StreamDetails.AudioSpecified Then
                                        For Each aStream In nTVEpisode.FileInfo.StreamDetails.Audio.Where(Function(f) f.LanguageSpecified AndAlso Not f.LongLanguageSpecified)
                                            aStream.LongLanguage = Localization.ISOGetLangByCode3(aStream.Language)
                                        Next
                                    End If
                                    If nTVEpisode.FileInfo.StreamDetails.SubtitleSpecified Then
                                        For Each sStream In nTVEpisode.FileInfo.StreamDetails.Subtitle.Where(Function(f) f.LanguageSpecified AndAlso Not f.LongLanguageSpecified)
                                            sStream.LongLanguage = Localization.ISOGetLangByCode3(sStream.Language)
                                        Next
                                    End If
                                End If
                                Return nTVEpisode
                            End Using
                        ElseIf rMatches.Count > 1 Then
                            For Each xmlReg As Match In rMatches
                                Using xmlRead As StringReader = New StringReader(xmlReg.Value)
                                    nTVEpisode = DirectCast(xmlSer.Deserialize(xmlRead), MediaContainers.MainDetails)
                                    nTVEpisode = CleanNFO_TVEpisodes(nTVEpisode)
                                    If nTVEpisode.Episode = EpisodeNumber AndAlso nTVEpisode.Season = SeasonNumber Then
                                        xmlSer = Nothing
                                        Return nTVEpisode
                                    End If
                                End Using
                            Next
                        End If
                    End Using

                Else
                    'not really anything else to do with non-conforming nfos aside from rename them
                    If Not Master.eSettings.GeneralOverwriteNfo Then
                        RenameNonConfNFO_TVEpisode(sPath, True)
                    End If
                End If

            Catch ex As Exception
                logger.Error(ex, New StackFrame().GetMethod().Name)

                'not really anything else to do with non-conforming nfos aside from rename them
                If Not Master.eSettings.GeneralOverwriteNfo Then
                    RenameNonConfNFO_TVEpisode(sPath, True)
                End If
            End Try
        End If

        Return New MediaContainers.MainDetails
    End Function

    Public Shared Function LoadFromNFO_TVEpisode(ByVal sPath As String, ByVal SeasonNumber As Integer, ByVal Aired As String) As MediaContainers.MainDetails
        Dim xmlRootAtt As New XmlRootAttribute With {.ElementName = "episodedetails"}
        Dim xmlSer As XmlSerializer = New XmlSerializer(GetType(MediaContainers.MainDetails), xmlRootAtt)
        Dim nTVEpisode As New MediaContainers.MainDetails

        If Not String.IsNullOrEmpty(sPath) AndAlso SeasonNumber >= -1 Then
            Try
                If File.Exists(sPath) Then
                    'better way to read multi-root xml??
                    Using xmlSR As StreamReader = New StreamReader(sPath)
                        Dim xmlStr As String = xmlSR.ReadToEnd
                        Dim rMatches As MatchCollection = Regex.Matches(xmlStr, "<episodedetails.*?>.*?</episodedetails>", RegexOptions.IgnoreCase Or RegexOptions.Singleline Or RegexOptions.IgnorePatternWhitespace)
                        If rMatches.Count = 1 Then
                            'only one episodedetail... assume it's the proper one
                            Using xmlRead As StringReader = New StringReader(rMatches(0).Value)
                                nTVEpisode = DirectCast(xmlSer.Deserialize(xmlRead), MediaContainers.MainDetails)
                                nTVEpisode = CleanNFO_TVEpisodes(nTVEpisode)
                                xmlSer = Nothing
                                If nTVEpisode.FileInfoSpecified Then
                                    If nTVEpisode.FileInfo.StreamDetails.AudioSpecified Then
                                        For Each aStream In nTVEpisode.FileInfo.StreamDetails.Audio.Where(Function(f) f.LanguageSpecified AndAlso Not f.LongLanguageSpecified)
                                            aStream.LongLanguage = Localization.ISOGetLangByCode3(aStream.Language)
                                        Next
                                    End If
                                    If nTVEpisode.FileInfo.StreamDetails.SubtitleSpecified Then
                                        For Each sStream In nTVEpisode.FileInfo.StreamDetails.Subtitle.Where(Function(f) f.LanguageSpecified AndAlso Not f.LongLanguageSpecified)
                                            sStream.LongLanguage = Localization.ISOGetLangByCode3(sStream.Language)
                                        Next
                                    End If
                                End If
                                Return nTVEpisode
                            End Using
                        ElseIf rMatches.Count > 1 Then
                            For Each xmlReg As Match In rMatches
                                Using xmlRead As StringReader = New StringReader(xmlReg.Value)
                                    nTVEpisode = DirectCast(xmlSer.Deserialize(xmlRead), MediaContainers.MainDetails)
                                    nTVEpisode = CleanNFO_TVEpisodes(nTVEpisode)
                                    If nTVEpisode.Aired = Aired AndAlso nTVEpisode.Season = SeasonNumber Then
                                        xmlSer = Nothing
                                        Return nTVEpisode
                                    End If
                                End Using
                            Next
                        End If
                    End Using

                Else
                    'not really anything else to do with non-conforming nfos aside from rename them
                    If Not Master.eSettings.GeneralOverwriteNfo Then
                        RenameNonConfNFO_TVEpisode(sPath, True)
                    End If
                End If

            Catch ex As Exception
                logger.Error(ex, New StackFrame().GetMethod().Name)

                'not really anything else to do with non-conforming nfos aside from rename them
                If Not Master.eSettings.GeneralOverwriteNfo Then
                    RenameNonConfNFO_TVEpisode(sPath, True)
                End If
            End Try
        End If

        Return New MediaContainers.MainDetails
    End Function

    Public Shared Function LoadFromNFO_TVShow(ByVal sPath As String) As MediaContainers.MainDetails
        Dim xmlRootAtt As New XmlRootAttribute With {.ElementName = "tvshow"}
        Dim xmlSer As XmlSerializer = New XmlSerializer(GetType(MediaContainers.MainDetails), xmlRootAtt)
        Dim nShow As New MediaContainers.MainDetails

        If Not String.IsNullOrEmpty(sPath) Then
            Try
                If File.Exists(sPath) Then
                    Using xmlSR As StreamReader = New StreamReader(sPath)
                        nShow = DirectCast(xmlSer.Deserialize(xmlSR), MediaContainers.MainDetails)
                        nShow = CleanNFO_TVShow(nShow)
                    End Using
                Else
                    'not really anything else to do with non-conforming nfos aside from rename them
                    If Not Master.eSettings.GeneralOverwriteNfo Then
                        RenameNonConfNFO_TVShow(sPath)
                    End If
                End If

            Catch ex As Exception
                logger.Error(ex, New StackFrame().GetMethod().Name)

                'not really anything else to do with non-conforming nfos aside from rename them
                If Not Master.eSettings.GeneralOverwriteNfo Then
                    RenameNonConfNFO_TVShow(sPath)
                End If
            End Try

            Try
                Dim params As New List(Of Object)(New Object() {nShow})
                Dim doContinue As Boolean = True
                'AddonsManager.Instance.RunGeneric(Enums.AddonEventType.OnNFORead_TVShow, params, doContinue, False)

            Catch ex As Exception
                logger.Error(ex, New StackFrame().GetMethod().Name)
            End Try

            If xmlSer IsNot Nothing Then
                xmlSer = Nothing
            End If
        End If

        Return nShow
    End Function

    Private Shared Sub RenameNonConfNFO_Movie(ByVal sPath As String, ByVal isChecked As Boolean)
        'test if current nfo is non-conforming... rename per setting

        Try
            If isChecked OrElse Not IsConformingNFO_Movie(sPath) Then
                If isChecked OrElse File.Exists(sPath) Then
                    RenameToInfo(sPath)
                End If
            End If
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    Private Shared Sub RenameNonConfNFO_TVEpisode(ByVal sPath As String, ByVal isChecked As Boolean)
        'test if current nfo is non-conforming... rename per setting

        Try
            If File.Exists(sPath) AndAlso Not IsConformingNFO_TVEpisode(sPath) Then
                RenameToInfo(sPath)
            End If
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    Private Shared Sub RenameNonConfNFO_TVShow(ByVal sPath As String)
        'test if current nfo is non-conforming... rename per setting

        Try
            If File.Exists(sPath) AndAlso Not IsConformingNFO_TVShow(sPath) Then
                RenameToInfo(sPath)
            End If
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    Private Shared Sub RenameToInfo(ByVal sPath As String)
        Try
            Dim i As Integer = 1
            Dim strNewName As String = String.Concat(FileUtils.Common.RemoveExtFromPath(sPath), ".info")
            'in case there is already a .info file
            If File.Exists(strNewName) Then
                Do
                    strNewName = String.Format("{0}({1}).info", FileUtils.Common.RemoveExtFromPath(sPath), i)
                    i += 1
                Loop While File.Exists(strNewName)
                strNewName = String.Format("{0}({1}).info", FileUtils.Common.RemoveExtFromPath(sPath), i)
            End If
            My.Computer.FileSystem.RenameFile(sPath, Path.GetFileName(strNewName))
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    Public Shared Sub SaveToNFO_Movie(ByRef tDBElement As Database.DBElement, ByVal ForceFileCleanup As Boolean)
        Dim xmlRootAtt As New XmlRootAttribute With {.ElementName = "movie"}
        Dim xmlSer As XmlSerializer = New XmlSerializer(GetType(MediaContainers.MainDetails), xmlRootAtt)

        Try
            Try
                Dim params As New List(Of Object)(New Object() {tDBElement})
                Dim doContinue As Boolean = True
                'AddonsManager.Instance.RunGeneric(Enums.AddonEventType.OnNFOSave_Movie, params, doContinue, False)
                If Not doContinue Then Return
            Catch ex As Exception
                logger.Error(ex, New StackFrame().GetMethod().Name)
            End Try

            If tDBElement.FilenameSpecified Then
                'cleanup old NFOs if needed
                If ForceFileCleanup Then DeleteNFO_Movie(tDBElement, ForceFileCleanup)

                'Create a clone of MediaContainer to prevent changes on database data that only needed in NFO
                Dim tMovie As MediaContainers.MainDetails = CType(tDBElement.MainDetails.CloneDeep, MediaContainers.MainDetails)

                Dim doesExist As Boolean = False
                Dim fAtt As New FileAttributes
                Dim fAttWritable As Boolean = True

                'YAMJ support
                If Master.eSettings.MovieUseYAMJ AndAlso Master.eSettings.MovieNFOYAMJ Then
                    If tMovie.TMDBSpecified Then
                        tMovie.TMDB = -1
                    End If
                End If

                'digit grouping symbol for Votes count
                If Master.eSettings.GeneralDigitGrpSymbolVotes Then
                    If tMovie.VotesSpecified Then
                        Dim vote As String = Double.Parse(tMovie.Votes, Globalization.CultureInfo.InvariantCulture).ToString("N0", Globalization.CultureInfo.CurrentCulture)
                        If vote IsNot Nothing Then tMovie.Votes = vote
                    End If
                End If

                For Each a In FileUtils.GetFilenameList.Movie(tDBElement, Enums.ScrapeModifierType.MainNFO)
                    If Not Master.eSettings.GeneralOverwriteNfo Then
                        RenameNonConfNFO_Movie(a, False)
                    End If

                    doesExist = File.Exists(a)
                    If Not doesExist OrElse (Not CBool(File.GetAttributes(a) And FileAttributes.ReadOnly)) Then
                        If doesExist Then
                            fAtt = File.GetAttributes(a)
                            Try
                                File.SetAttributes(a, FileAttributes.Normal)
                            Catch ex As Exception
                                fAttWritable = False
                            End Try
                        End If
                        Using xmlSW As New StreamWriter(a)
                            tDBElement.NfoPath = a
                            xmlSer.Serialize(xmlSW, tMovie)
                        End Using
                        If doesExist And fAttWritable Then File.SetAttributes(a, fAtt)
                    End If
                Next
            End If

        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    Public Shared Sub SaveToNFO_MovieSet(ByRef tDBElement As Database.DBElement)
        Dim xmlRootAtt As New XmlRootAttribute With {.ElementName = "movieset"}
        Dim xmlSer As XmlSerializer = New XmlSerializer(GetType(MediaContainers.MainDetails), xmlRootAtt)

        Try
            'Try
            '    Dim params As New List(Of Object)(New Object() {moviesetToSave})
            '    Dim doContinue As Boolean = True
            '    ModulesManager.Instance.RunGeneric(Enums.ModuleEventType.OnMovieSetNFOSave, params, doContinue, False)
            '    If Not doContinue Then Return
            'Catch ex As Exception
            'End Try

            If Not String.IsNullOrEmpty(tDBElement.MainDetails.Title) Then
                If tDBElement.MainDetails.TitleHasChanged Then DeleteNFO_MovieSet(tDBElement, False, True)

                Dim doesExist As Boolean = False
                Dim fAtt As New FileAttributes
                Dim fAttWritable As Boolean = True

                For Each a In FileUtils.GetFilenameList.MovieSet(tDBElement, Enums.ScrapeModifierType.MainNFO)
                    'If Not Master.eSettings.GeneralOverwriteNfo Then
                    '    RenameNonConfNfo(a, False)
                    'End If

                    doesExist = File.Exists(a)
                    If Not doesExist OrElse (Not CBool(File.GetAttributes(a) And FileAttributes.ReadOnly)) Then
                        If doesExist Then
                            fAtt = File.GetAttributes(a)
                            Try
                                File.SetAttributes(a, FileAttributes.Normal)
                            Catch ex As Exception
                                fAttWritable = False
                            End Try
                        End If
                        Using xmlSW As New StreamWriter(a)
                            tDBElement.NfoPath = a
                            xmlSer.Serialize(xmlSW, tDBElement.MainDetails)
                        End Using
                        If doesExist And fAttWritable Then File.SetAttributes(a, fAtt)
                    End If
                Next
            End If

        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    Public Shared Sub SaveToNFO_TVEpisode(ByRef tDBElement As Database.DBElement)
        Dim xmlRootAtt As New XmlRootAttribute With {.ElementName = "episodedetails"}
        Dim xmlSer As XmlSerializer = New XmlSerializer(GetType(MediaContainers.MainDetails), xmlRootAtt)

        Try
            If tDBElement.FilenameSpecified Then
                'Create a clone of MediaContainer to prevent changes on database data that only needed in NFO
                Dim tTVEpisode As MediaContainers.MainDetails = CType(tDBElement.MainDetails.CloneDeep, MediaContainers.MainDetails)

                Dim doesExist As Boolean = False
                Dim fAtt As New FileAttributes
                Dim fAttWritable As Boolean = True
                Dim EpList As New List(Of MediaContainers.MainDetails)
                Dim sBuilder As New StringBuilder

                For Each a In FileUtils.GetFilenameList.TVEpisode(tDBElement, Enums.ScrapeModifierType.EpisodeNFO)
                    If Not Master.eSettings.GeneralOverwriteNfo Then
                        RenameNonConfNFO_TVEpisode(a, False)
                    End If

                    doesExist = File.Exists(a)
                    If Not doesExist OrElse (Not CBool(File.GetAttributes(a) And FileAttributes.ReadOnly)) Then

                        If doesExist Then
                            fAtt = File.GetAttributes(a)
                            Try
                                File.SetAttributes(a, FileAttributes.Normal)
                            Catch ex As Exception
                                fAttWritable = False
                            End Try
                        End If

                        Using SQLCommand As SQLite.SQLiteCommand = Master.DB.MyVideosDBConn.CreateCommand()
                            SQLCommand.CommandText = "SELECT idEpisode FROM episode WHERE idEpisode <> (?) AND idFile IN (SELECT idFile FROM files WHERE strFilename = (?)) ORDER BY Episode"
                            Dim parID As SQLite.SQLiteParameter = SQLCommand.Parameters.Add("parID", DbType.Int64, 0, "idEpisode")
                            Dim parFilename As SQLite.SQLiteParameter = SQLCommand.Parameters.Add("parFilename", DbType.String, 0, "strFilename")

                            parID.Value = tDBElement.ID
                            parFilename.Value = tDBElement.Filename

                            Using SQLreader As SQLite.SQLiteDataReader = SQLCommand.ExecuteReader
                                While SQLreader.Read
                                    EpList.Add(Master.DB.Load_TVEpisode(Convert.ToInt64(SQLreader("idEpisode")), False).MainDetails)
                                End While
                            End Using

                            EpList.Add(tTVEpisode)

                            Dim NS As New XmlSerializerNamespaces
                            NS.Add(String.Empty, String.Empty)

                            For Each tvEp As MediaContainers.MainDetails In EpList.OrderBy(Function(s) s.Season).OrderBy(Function(e) e.Episode)

                                'digit grouping symbol for Votes count
                                If Master.eSettings.GeneralDigitGrpSymbolVotes Then
                                    If tvEp.VotesSpecified Then
                                        Dim vote As String = Double.Parse(tvEp.Votes, Globalization.CultureInfo.InvariantCulture).ToString("N0", Globalization.CultureInfo.CurrentCulture)
                                        If vote IsNot Nothing Then tvEp.Votes = vote
                                    End If
                                End If

                                'removing <displayepisode> and <displayseason> if disabled
                                If Not Master.eSettings.TVScraperUseDisplaySeasonEpisode Then
                                    tvEp.DisplayEpisode = -1
                                    tvEp.DisplaySeason = -1
                                End If

                                Using xmlSW As New Utf8StringWriter
                                    xmlSer.Serialize(xmlSW, tvEp, NS)
                                    If sBuilder.Length > 0 Then
                                        sBuilder.Append(Environment.NewLine)
                                        xmlSW.GetStringBuilder.Remove(0, xmlSW.GetStringBuilder.ToString.IndexOf(Environment.NewLine) + 1)
                                    End If
                                    sBuilder.Append(xmlSW.ToString)
                                End Using
                            Next

                            tDBElement.NfoPath = a

                            If sBuilder.Length > 0 Then
                                Using fSW As New StreamWriter(a)
                                    fSW.Write(sBuilder.ToString)
                                End Using
                            End If
                        End Using
                        If doesExist And fAttWritable Then File.SetAttributes(a, fAtt)
                    End If
                Next
            End If

        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    Public Shared Sub SaveToNFO_TVShow(ByRef tDBElement As Database.DBElement)
        Dim xmlRootAtt As New XmlRootAttribute With {.ElementName = "tvshow"}
        Dim xmlSer As XmlSerializer = New XmlSerializer(GetType(MediaContainers.MainDetails), xmlRootAtt)

        Try
            Dim params As New List(Of Object)(New Object() {tDBElement})
            Dim doContinue As Boolean = True
            'AddonsManager.Instance.RunGeneric(Enums.AddonEventType.OnNFOSave_TVShow, params, doContinue, False)
            If Not doContinue Then Return
        Catch ex As Exception
        End Try

        Try
            If tDBElement.ShowPathSpecified Then
                'Create a clone of MediaContainer to prevent changes on database data that only needed in NFO
                Dim tTVShow As MediaContainers.MainDetails = CType(tDBElement.MainDetails.CloneDeep, MediaContainers.MainDetails)

                Dim doesExist As Boolean = False
                Dim fAtt As New FileAttributes
                Dim fAttWritable As Boolean = True

                'Boxee support
                If Master.eSettings.TVUseBoxee Then
                    If tTVShow.TVDBSpecified() Then
                        tTVShow.BoxeeTvDb = CStr(tTVShow.TVDB)
                        tTVShow.BlankId()
                    End If
                End If

                'digit grouping symbol for Votes count
                If Master.eSettings.GeneralDigitGrpSymbolVotes Then
                    If tTVShow.VotesSpecified Then
                        Dim vote As String = Double.Parse(tTVShow.Votes, Globalization.CultureInfo.InvariantCulture).ToString("N0", Globalization.CultureInfo.CurrentCulture)
                        If vote IsNot Nothing Then tTVShow.Votes = vote
                    End If
                End If

                For Each a In FileUtils.GetFilenameList.TVShow(tDBElement, Enums.ScrapeModifierType.MainNFO)
                    If Not Master.eSettings.GeneralOverwriteNfo Then
                        RenameNonConfNFO_TVShow(a)
                    End If

                    doesExist = File.Exists(a)
                    If Not doesExist OrElse (Not CBool(File.GetAttributes(a) And FileAttributes.ReadOnly)) Then

                        If doesExist Then
                            fAtt = File.GetAttributes(a)
                            Try
                                File.SetAttributes(a, FileAttributes.Normal)
                            Catch ex As Exception
                                fAttWritable = False
                            End Try
                        End If

                        Using xmlSW As New StreamWriter(a)
                            tDBElement.NfoPath = a
                            xmlSer.Serialize(xmlSW, tTVShow)
                        End Using

                        If doesExist And fAttWritable Then File.SetAttributes(a, fAtt)
                    End If
                Next
            End If
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

#End Region 'Methods

#Region "Nested Types"

    Public Class NonConf

#Region "Fields"

        Private _imdbid As String
        Private _text As String

#End Region 'Fields

#Region "Constructors"

        Public Sub New()
            Clear()
        End Sub

#End Region 'Constructors

#Region "Properties"

        Public Property IMDBID() As String
            Get
                Return _imdbid
            End Get
            Set(ByVal value As String)
                _imdbid = value
            End Set
        End Property

        Public Property Text() As String
            Get
                Return _text
            End Get
            Set(ByVal value As String)
                _text = value
            End Set
        End Property

#End Region 'Properties

#Region "Methods"

        Public Sub Clear()
            _imdbid = String.Empty
            _text = String.Empty
        End Sub

#End Region 'Methods

    End Class

    Public Class KnownEpisode

#Region "Fields"

        Private _aireddate As String
        Private _episode As Integer
        Private _episodeabsolute As Integer
        Private _episodecombined As Double
        Private _episodedvd As Double
        Private _season As Integer
        Private _seasoncombined As Integer
        Private _seasondvd As Integer

#End Region 'Fields

#Region "Constructors"

        Public Sub New()
            Clear()
        End Sub

#End Region 'Constructors

#Region "Properties"

        Public Property AiredDate() As String
            Get
                Return _aireddate
            End Get
            Set(ByVal value As String)
                _aireddate = value
            End Set
        End Property

        Public Property Episode() As Integer
            Get
                Return _episode
            End Get
            Set(ByVal value As Integer)
                _episode = value
            End Set
        End Property

        Public Property EpisodeAbsolute() As Integer
            Get
                Return _episodeabsolute
            End Get
            Set(ByVal value As Integer)
                _episodeabsolute = value
            End Set
        End Property

        Public Property EpisodeCombined() As Double
            Get
                Return _episodecombined
            End Get
            Set(ByVal value As Double)
                _episodecombined = value
            End Set
        End Property

        Public Property EpisodeDVD() As Double
            Get
                Return _episodedvd
            End Get
            Set(ByVal value As Double)
                _episodedvd = value
            End Set
        End Property

        Public Property Season() As Integer
            Get
                Return _season
            End Get
            Set(ByVal value As Integer)
                _season = value
            End Set
        End Property

        Public Property SeasonCombined() As Integer
            Get
                Return _seasoncombined
            End Get
            Set(ByVal value As Integer)
                _seasoncombined = value
            End Set
        End Property

        Public Property SeasonDVD() As Integer
            Get
                Return _seasondvd
            End Get
            Set(ByVal value As Integer)
                _seasondvd = value
            End Set
        End Property

#End Region 'Properties

#Region "Methods"

        Public Sub Clear()
            _aireddate = String.Empty
            _episode = -1
            _episodeabsolute = -1
            _episodecombined = -1
            _episodedvd = -1
            _season = -1
            _seasoncombined = -1
            _seasondvd = -1
        End Sub

#End Region 'Methods

    End Class

    Public NotInheritable Class Utf8StringWriter
        Inherits StringWriter
        Public Overloads Overrides ReadOnly Property Encoding() As Encoding
            Get
                Return Encoding.UTF8
            End Get
        End Property
    End Class

#End Region 'Nested Types

End Class