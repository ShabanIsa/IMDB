﻿using IMDB.Data.Context;
using IMDB.Data.Models;
using IMDB.Data.Views;
using IMDB.Services.Contracts;
using IMDB.Services.Exceptions;
using IMDB.Services.Providers;
using System.Collections.Generic;
using System.Linq;

namespace IMDB.Services
{
    public class ReviewsService : IReviewsServices
    {
        private IMDBContext context;
        private ILoginSession loginSession;
        private const int adminRank = 2;

        public ReviewsService(IMDBContext context, ILoginSession login)
        {
            this.context = context;
            loginSession = login;
        }

        public IEnumerable<ReviewView> ListMovieReviews(int movieID)
        {
            Validator.IsNonNegative(movieID, "MovieID cannot be negative");

            var foundMovie = context.Movies.FirstOrDefault(m => m.ID == movieID);

            if (foundMovie is null || foundMovie.IsDeleted == true)
            {
                throw new MovieNotFoundException("Movie not found.");
            }

            var reviewsQuery = context.Reviews
                                    .Where(r => r.MovieID == movieID && r.IsDeleted == false)
                                    .Select(rev => new ReviewView()
                                    {
                                        ReviewID = rev.ID,
                                        Rating = rev.MovieRating,
                                        Score = rev.ReviewScore,
                                        Text = rev.Text,
                                        ByUser = rev.User.UserName,
                                        MovieName = rev.Movie.Name
                                    })
                                    .OrderByDescending(rev => rev.Score)
                                    .ToList();

            return reviewsQuery;
        }

        private double CalcualteRating(Review review, double newRating)
        {
            int count = context.ReviewRatings.Count(rev => rev.ReviewId == review.ID);
            double sumAllRatings = context.ReviewRatings.Where(rev => rev.ReviewId == review.ID).Sum(rev => rev.ReviewRating);
            return (sumAllRatings + newRating) / (count + 1);
        }

        public ReviewView RateReview(int reviewID, double rating)
        {
            Validator.IsNonNegative(reviewID, "ReviewID cannot be negative");
            Validator.IfIsInRangeInclusive(rating, 0D, 10D, "Score is in incorrect range.");

            var findReview = context.Reviews
                                         .Where(rev => rev.ID == reviewID && rev.IsDeleted == false)
                                         .Select(r => new Review()
                                         {
                                             ID = r.ID,
                                             IsDeleted = r.IsDeleted,
                                             MovieID = r.MovieID,
                                             MovieRating = r.MovieRating,
                                             ReviewScore = r.ReviewScore,
                                             Text = r.Text,
                                             UserID = r.UserID,
                                             Movie = r.Movie,
                                             User = r.User,
                                             ReviewRatings = r.ReviewRatings
                                         }
                                         ).FirstOrDefault();

            if (findReview is null)
            {
                throw new ReviewNotFoundException($"Review with ID: {reviewID} not found");
            }

            findReview.ReviewScore = CalcualteRating(findReview, rating);
            context.Reviews.Update(findReview);

            context.SaveChanges();

            var revView = new ReviewView()
            {
                ReviewID = findReview.ID,
                Rating = findReview.MovieRating,
                Score = findReview.ReviewScore,
                ByUser = findReview.User.UserName,
                MovieName = findReview.Movie.Name,
                Text = findReview.Text
            };

            return revView;
        }

        public void DeleteReview(int reviewID)
        {
            Validator.IsNonNegative(reviewID, "ReviewID cannot be negative.");

            var findReview = context.Reviews.FirstOrDefault(r => r.ID == reviewID);

            if (findReview is null)
            {
                throw new ReviewNotFoundException($"Review with ID: {reviewID} cannot be deleted. ID is invalid.");
            }

            if (findReview.User.ID == loginSession.LoggedUserID || (int)loginSession.LoggedUserRole == adminRank)
            {
                findReview.IsDeleted = true;
                context.Reviews.Update(findReview);
            }

            context.SaveChanges();
        }
    }
}
