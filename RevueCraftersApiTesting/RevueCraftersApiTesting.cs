using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using RevueCraftersApiTesting.Models;

namespace RevueCraftersApiTesting
{
    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string lastCreatedRevueId;

        private const string baseUrl = "https://d2925tksfvgq8c.cloudfront.net";

        private const string staticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJiYzliZTdjMi0xMGRiLTRjM2QtYWI0YS04ZWFlMmEyMTFjMTEiLCJpYXQiOiIwOC8yMi8yMDI1IDA2OjQ1OjA3IiwiVXNlcklkIjoiYTRlODU2N2YtOTRjZS00ODBlLTEzMzQtMDhkZGRlMWQ4YTY0IiwiRW1haWwiOiJ6aGVrb25pQHNtaXRoLmNvbSIsIlVzZXJOYW1lIjoiemhla29uaVNtaXRoIiwiZXhwIjoxNzU1ODY2NzA3LCJpc3MiOiJSZXZ1ZU1ha2VyX0FwcF9Tb2Z0VW5pIiwiYXVkIjoiUmV2dWVNYWtlcl9XZWJBUElfU29mdFVuaSJ9.rlQZQ_s7PI143GN-UPmr8ZkhR7VG2wdO0iRGYomBuLc";

        private const string loginEmail = "zhekoni@smith.com";
        private const string loginPassword = "zhekoni123";

        [OneTimeSetUp]
        public void Setup()
        {

            string jwtToken;

            if (!string.IsNullOrWhiteSpace(staticToken))
            {
                jwtToken = staticToken;
            }
            else
            {
                jwtToken = GetjwtToken(loginEmail, loginPassword);
            }

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };
            this.client = new RestClient(options);
        }

        private string GetjwtToken(string username, string password)
        {
            var tempClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { username, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);

                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrieve JWT token from the response.");
                }

                return token;
            }

            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code:{response.StatusCode}, Content: {response.Content}");
            }
        }

        [Test, Order(1)]
        public void CreateRevue_ShouldReturnSuccessfullyCreatedMsg()
        {
            var revueRequest = new RevueDTO
            {
                Title = "Test Revue by Zhekoni",
                Url = "",
                Description = "This is a test revue description"                
            };

            var request = new RestRequest("/api/Revue/Create", Method.Post);
            request.AddJsonBody(revueRequest);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Successfully created!"));
        }

        [Test, Order(2)]
        public void GetAllRevues_ShouldReturnNonEmptyArray()
        {
            var request = new RestRequest("/api/Revue/All", Method.Get);

            var response = this.client.Execute(request);

            var responseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response, Is.Not.Null);
            Assert.That(responseItems, Is.Not.Empty);

            lastCreatedRevueId = responseItems.LastOrDefault()?.RevueId;
        }

        [Test, Order(3)]
        public void EditLastRevue_ShouldReturnEditedSuccessfully()
        {
            RevueDTO editRequest = new RevueDTO
            {
                Title = "Edited title",
                Url = "",
                Description = "Edited description",
            };

            var request = new RestRequest("/api/Revue/Edit", Method.Put);
            request.AddQueryParameter("revueId", lastCreatedRevueId);
            request.AddJsonBody(editRequest);

            var response = this.client.Execute(request);

            //var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Edited successfully"));
        }

        [Test, Order(4)]
        public void DeleteLastRevue_ShouldReturnRevueIsDeleted()
        {
            var request = new RestRequest("/api/Revue/Delete", Method.Delete);
            request.AddQueryParameter("revueId", lastCreatedRevueId);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("The revue is deleted!"));
        }

        [Test, Order(5)]
        public void CreateARevueWithoutTheRequiredFields_ShouldReturnBadRequest()
        {
            RevueDTO badRevueRequest = new RevueDTO
            {
                Title = "",
                Url = "",
                Description = ""
            };

            var request = new RestRequest("/api/Revue/Create", Method.Post);
            request.AddJsonBody(badRevueRequest);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditANonExistingRevue_ShouldReturnBadRequest()
        {
            string fakeId = "112233";

            RevueDTO editRequest = new RevueDTO
            {
                Title = "Edited title",
                Url = "",
                Description = "Edited description",
            };

            var request = new RestRequest("/api/Revue/Edit", Method.Put);
            request.AddQueryParameter("revueId", fakeId);
            request.AddJsonBody(editRequest);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such revue!"));

        }

        [Test, Order(7)]
        public void DeleteANonExistingRevue_ShouldReturnBadRequest()
        {
            string fakeId = "123434";

            var request = new RestRequest("/api/Revue/Delete", Method.Delete);
            request.AddQueryParameter("revueId", fakeId);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such revue!"));

        }

            [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}