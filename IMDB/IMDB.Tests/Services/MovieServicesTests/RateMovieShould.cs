﻿using IMDB.Data.Models;
using IMDB.Data.Repository;
using IMDB.Services;
using IMDB.Services.Contracts;
using IMDB.Services.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IMDB.Tests.Services.MovieServicesTests
{
    [TestClass]
    public class RateMovieShould
    {
        [TestMethod]
        public void ThrowNotEnoughPermissionsException_WhenUserHasNotLoggedIn()
        {
            // Arrange
            var reviewRepoStub = new Mock<IRepository<Review>>();
            var movieRepoMock = new Mock<IRepository<Movie>>();
            var directorRepoStub = new Mock<IRepository<Director>>();
            var genreRepoStub = new Mock<IRepository<Genre>>();
            var movieGenreRepoStub = new Mock<IRepository<MovieGenre>>();

            var loginSessionMock = new Mock<ILoginSession>();
            loginSessionMock
                .SetupGet(ls => ls.LoggedUserPermissions)
                .Returns(new List<string>() { "cmd0", "cmd1", "butnotcmdratemovie" });

            var sut = new MovieServices(reviewRepoStub.Object, movieRepoMock.Object, directorRepoStub.Object, genreRepoStub.Object, movieGenreRepoStub.Object, loginSessionMock.Object);
            // Act & Assert
            Assert.ThrowsException<NotEnoughPermissionException>(() => sut.RateMovie(1, 1.0, "pishki"));
        }
        [DataTestMethod]
        [DataRow(-1, 1)]
        [DataRow(0, 1)]
        [DataRow(2, -1)]
        [DataRow(2, 11)]
        public void ThrowArgumentException_WhenArgumentsAreIncorrect(int movieID, int rating)
        {
            // Arrange
            var reviewRepoStub = new Mock<IRepository<Review>>();

            var movieRepoMock = new Mock<IRepository<Movie>>();
            var directorRepoStub = new Mock<IRepository<Director>>();
            var genreRepoStub = new Mock<IRepository<Genre>>();
            var movieGenreRepoStub = new Mock<IRepository<MovieGenre>>();

            var loginSessionMock = new Mock<ILoginSession>();
            loginSessionMock
                .SetupGet(ls => ls.LoggedUserPermissions)
                .Returns(new List<string>() { "cmd0", "cmd1", "ratemovie" });

            var sut = new MovieServices(reviewRepoStub.Object, movieRepoMock.Object, directorRepoStub.Object, genreRepoStub.Object, movieGenreRepoStub.Object, loginSessionMock.Object);
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => sut.RateMovie(movieID, rating, "pishki"));
        }
        [DataTestMethod]
        [DataRow(1, true)]
        [DataRow(2, false)]
        public void ThrowMovieNotFoundExceptionWhenMovieIsNotValid(int movieID, bool deletedFlag)
        {
            // Arrange
            var reviewRepoStub = new Mock<IRepository<Review>>();

            var movieRepoMock = new Mock<IRepository<Movie>>();
            var movieMock = new Movie
            {
                Name = "Stivi's adventure into unit testing of the underworld",
                ID = 1,
                IsDeleted = deletedFlag,
            };
            movieRepoMock
                 .Setup(mr => mr.All())
                 .Returns(new List<Movie>() { movieMock }.AsQueryable());

            var directorRepoStub = new Mock<IRepository<Director>>();
            var genreRepoStub = new Mock<IRepository<Genre>>();
            var movieGenreRepoStub = new Mock<IRepository<MovieGenre>>();

            var loginSessionMock = new Mock<ILoginSession>();
            loginSessionMock
                .SetupGet(ls => ls.LoggedUserPermissions)
                .Returns(new List<string>() { "cmd0", "cmd1", "ratemovie" });

            var sut = new MovieServices(reviewRepoStub.Object, movieRepoMock.Object, directorRepoStub.Object, genreRepoStub.Object, movieGenreRepoStub.Object, loginSessionMock.Object);
            // Act & Assert
            Assert.ThrowsException<MovieNotFoundException>(() => sut.RateMovie(movieID, 5, "pishki"));
        }
        [TestMethod]
        public void UpdateMovieScoreCreatingReview_WhenParametersAreCorrect()
        {
            // Arrange
            const int movieID = 2;
            const int loggedUserID = 12;
            const int ratingInput = 8;
            const string textInput = "pishki";
            const int r1rating = 7;
            const int r2rating = 3;
            const int r3DeleteDrating = 3;
            const int movieScore = 5;


            var movieRepoMock = new Mock<IRepository<Movie>>();
            var review1 = new Review { MovieID = movieID, MovieRating = r1rating, UserID = 123123123 };
            var review2 = new Review { MovieID = movieID, MovieRating = r2rating, UserID = 12352315 };
            var review3Deleted = new Review { MovieID = movieID, MovieRating = r3DeleteDrating, IsDeleted = true };
            var movie = new Movie
            {
                Name = "Stivi's adventure into unit testing of the underworld",
                ID = movieID,
                IsDeleted = false,
                MovieScore = movieScore,
                Reviews = new List<Review>() { review1, review2, review3Deleted }
            };
            movieRepoMock
                 .Setup(mr => mr.All())
                 .Returns(new List<Movie>() { movie }.AsQueryable());

            var reviewRepoMock = new Mock<IRepository<Review>>();
            Review createdReviewBySut = null;
            reviewRepoMock
                .Setup(rr => rr.Add(It.IsAny<Review>()))
                .Callback<Review>(r => createdReviewBySut = r);
            reviewRepoMock
                .Setup(rr => rr.All())
                .Returns(new List<Review>() { review1, review2, review3Deleted }.AsQueryable());

            var directorRepoStub = new Mock<IRepository<Director>>();
            var genreRepoStub = new Mock<IRepository<Genre>>();
            var movieGenreRepoStub = new Mock<IRepository<MovieGenre>>();

            var loginSessionMock = new Mock<ILoginSession>();
            loginSessionMock
                .SetupGet(ls => ls.LoggedUserPermissions)
                .Returns(new List<string>() { "cmd0", "cmd1", "ratemovie" });
            loginSessionMock
                .SetupGet(ls => ls.LoggedUserID)
                .Returns(loggedUserID);

            var sut = new MovieServices(reviewRepoMock.Object, movieRepoMock.Object, directorRepoStub.Object, genreRepoStub.Object, movieGenreRepoStub.Object, loginSessionMock.Object);
            // Act
            sut.RateMovie(movieID, ratingInput, textInput);
            //Assert
            //check if movie's score is updated should be 6
            Assert.IsTrue(movie.MovieScore == (r1rating + r2rating + ratingInput) / 3);
            Assert.IsTrue(createdReviewBySut.MovieID == movieID);
            Assert.IsTrue(createdReviewBySut.MovieRating == ratingInput);
            Assert.IsTrue(createdReviewBySut.UserID == loggedUserID);
            Assert.IsTrue(createdReviewBySut.Text == textInput);
            movieRepoMock.Verify(mr => mr.Update(movie));
            movieRepoMock.Verify(mr => mr.Save(), Times.Once);
        }
        [TestMethod]
        public void UpdateMovieScoreUpdatingReview_WhenParametersAreCorrect()
        {
            // Arrange
            const int movieID = 2;
            const int loggedUserID = 12;
            const int ratingInput = 8;
            const string textInput = "pishki";
            const int r1rating = 7;
            const int r2rating = 3;
            const int r3DeleteDrating = 3;
            const int movieScore = 5;


            var movieRepoMock = new Mock<IRepository<Movie>>();
            var review1ToBeUpdated = new Review { MovieID = movieID, MovieRating = r1rating, UserID = loggedUserID };
            var review2 = new Review { MovieID = movieID, MovieRating = r2rating, UserID = 124124};
            var review3Deleted = new Review { MovieID = movieID, MovieRating = r3DeleteDrating, IsDeleted = true };
            var movie = new Movie
            {
                Name = "Stivi's adventure into unit testing of the underworld",
                ID = movieID,
                IsDeleted = false,
                MovieScore = movieScore,
                Reviews = new List<Review>() { review1ToBeUpdated, review2, review3Deleted }
            };
            movieRepoMock
                 .Setup(mr => mr.All())
                 .Returns(new List<Movie>() { movie }.AsQueryable());

            var reviewRepoMock = new Mock<IRepository<Review>>();
            reviewRepoMock
                .Setup(rr => rr.All())
                .Returns(new List<Review>() { review1ToBeUpdated, review2, review3Deleted }.AsQueryable());

            var directorRepoStub = new Mock<IRepository<Director>>();
            var genreRepoStub = new Mock<IRepository<Genre>>();
            var movieGenreRepoStub = new Mock<IRepository<MovieGenre>>();

            var loginSessionMock = new Mock<ILoginSession>();
            loginSessionMock
                .SetupGet(ls => ls.LoggedUserPermissions)
                .Returns(new List<string>() { "cmd0", "cmd1", "ratemovie" });
            loginSessionMock
                .SetupGet(ls => ls.LoggedUserID)
                .Returns(loggedUserID);

            var sut = new MovieServices(reviewRepoMock.Object, movieRepoMock.Object, directorRepoStub.Object, genreRepoStub.Object, movieGenreRepoStub.Object, loginSessionMock.Object);
            // Act
            sut.RateMovie(movieID, ratingInput, textInput);
            //Assert
            //check if movie's score is updated should be 6
            Assert.IsTrue(movie.MovieScore == (double)(r1rating - r1rating + r2rating + ratingInput) / 2);
            Assert.IsTrue(review1ToBeUpdated.MovieID == movieID);
            Assert.IsTrue(review1ToBeUpdated.MovieRating == ratingInput);
            Assert.IsTrue(review1ToBeUpdated.UserID == loggedUserID);
            Assert.IsTrue(review1ToBeUpdated.Text == textInput);
            movieRepoMock.Verify(mr => mr.Update(movie));
            movieRepoMock.Verify(mr => mr.Save(), Times.Once);
            reviewRepoMock.Verify(mr => mr.Update(It.IsAny<Review>()), Times.Once);
        }
    }
}