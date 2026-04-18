using System;
using System.Net;
using System.Text.Json;
using RestSharp;
using RestSharp.Authenticators;
using MovieCatalog.Models;

namespace MovieCatalog
{
    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string lastCreatedMovieId;

        private const string BaseUrl = "http://144.91.123.158:5000";
        private const string AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJmODQzN2QxYi0zZDMyLTQ0MGEtYjBiYS03NDUyNzE4OTUzNGQiLCJpYXQiOiIwNC8xOC8yMDI2IDA2OjU3OjM5IiwiVXNlcklkIjoiMzA5MDEyM2ItOWVkYS00M2I4LTYyOTgtMDhkZTc2OTcxYWI5IiwiRW1haWwiOiJ0ZXN0X3VzZXJfUUFAZW1haWwuY29tIiwiVXNlck5hbWUiOiJtb3ZpZV9tYW5pYWMiLCJleHAiOjE3NzY1MTcwNTksImlzcyI6Ik1vdmllQ2F0YWxvZ19BcHBfU29mdFVuaSIsImF1ZCI6Ik1vdmllQ2F0YWxvZ19XZWJBUElfU29mdFVuaSJ9.uwlxpSnlhAeOhBWR9yKsam7pFXncj-tEcpAuHmOO9DI";
        private const string Email = "test_user_QA@email.com";
        private const string Password = "pass123";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = string.IsNullOrWhiteSpace(AccessToken)
                ? GetJwtToken(Email, Password)
                : AccessToken;

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("token").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Authentication succeeded but token was empty.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Authentication failed. Status: {response.StatusCode}, Body: {response.Content}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateMovie_WithValidRequiredFields_ShouldReturnOK()
        {
            var movieData = new MovieDto
            {
                Title = "Full Moon Horror",
                Description = "Don't watch this movie before going to bed"
            };

            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movieData);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            var result = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);
            Assert.That(result.Movie, Is.Not.Null, "The response should contain the created movie object.");
            Assert.That(result.Movie.Id, Is.Not.Null.Or.Empty, "The returned movie ID should not be null or empty.");
            Assert.That(result.Msg, Is.EqualTo("Movie created successfully!"));

            lastCreatedMovieId = result.Movie.Id;
        }

        [Order(2)]
        [Test]
        public void EditMovie_WithValidData_ShouldUpdateMovieSuucessfully()
        {
            var editMovieData = new MovieDto
            {
                Title = "Full Moon Horror Edited",
                Description = "I shouldn't have watched it..."
            };

            var request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", lastCreatedMovieId);
            request.AddJsonBody(editMovieData);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            var result = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);
            Assert.That(result.Msg, Is.EqualTo("Movie edited successfully!"));
        }

        [Order(3)]
        [Test]
        public void GetAllMovies_ShouldReturnNonEmptyList()
        {
            var request = new RestRequest("/api/Catalog/All", Method.Get);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            var movies = JsonSerializer.Deserialize<List<MovieDto>>(response.Content);
            Assert.That(movies, Is.Not.Null.And.Not.Empty, "The movie catalog was empty or failed to parse.");
            Assert.That(movies.Count, Is.GreaterThanOrEqualTo(1), "The movie catalog contains at least one element.");
        }

        [Order(4)]
        [Test]
        public void DeleteMovie_WithValidId_ShouldRemoveMovieSuccessfully()
        {
            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", lastCreatedMovieId);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            var result = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);
            Assert.That(result.Msg, Is.EqualTo("Movie deleted successfully!"));
        }

        [Order(5)]
        [Test]
        public void CreateMovie_WithEmptyRequiredFields_ShouldReturnBadRequest()
        {
            var invalidMovieData = new MovieDto
            {
                Title = "",
                Description = ""
            };

            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(invalidMovieData);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
        }

        [Order(6)]
        [Test]
        public void EditMovie_WithNonExistientId_ShouldReturnBadRequest()
        {
            string nonExistentMovieId = "invalid_id_12321";

            var movieData = new MovieDto
            {
                Title = "Is there a pilot on the plane?",
                Description = "I have really bad taste in movies."
            };

            var request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", nonExistentMovieId);
            request.AddJsonBody(movieData);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
            var result = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);
            Assert.That(result.Msg, Is.EqualTo("Unable to edit the movie! Check the movieId parameter or user verification!"));
        }

        [Order(7)]
        [Test]
        public void DeleteMovie_WithNonExistentId_ShouldReturnBadRequest()
        {
            string nonExistentMovieId = "invalid_id_12321";

            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", nonExistentMovieId);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
            var result = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);
            Assert.That(result.Msg, Is.EqualTo("Unable to delete the movie! Check the movieId parameter or user verification!"));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }

        //Adding this comment to trigger the CI
    }
}
