﻿using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Utility
{
    public static class HttpClientHelper
    {
        public static CookieContainer CookieContainer = new CookieContainer();
        public static async Task<T> PostAsJsonAsync<T>(string host, string path, Object data)
        {
            string stringData = JsonConvert.SerializeObject(data);
            var contentData = new StringContent(stringData, Encoding.UTF8, "application/json");

            using (var client = GetHttpClient())
            {
                client.BaseAddress = new Uri(host);
                HttpResponseMessage response = await client.PostAsync(path, contentData);
                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<T>(stream);
                }
            }

            return default(T);
        }

        public static async Task<T> GetAsJsonAsync<T>(string host, string path)
        {
            using (var client = GetHttpClient())
            {
                client.BaseAddress = new Uri(host);
                HttpResponseMessage response = await client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<T>(stream);
                }
            }

            return default(T);
        }

        public static HttpClient GetHttpClient()
        {
            HttpClientHandler handler = new HttpClientHandler()
            {
                // Use shared cookie container to autherticate user identity.
                CookieContainer = CookieContainer,
                UseCookies = true,
                UseDefaultCredentials = false
            };

            return new HttpClient(handler);
        }

        public static Task<IRestResponse> Execute(this IRestClient restClient, RestRequest restRequest)
        {
            var tcs = new TaskCompletionSource<IRestResponse>();
            restClient.ExecuteAsync(restRequest, (restResponse, asyncHandle) =>
            {
                if (restResponse.ResponseStatus == ResponseStatus.Error)
                    tcs.SetException(restResponse.ErrorException);
                else
                    tcs.SetResult(restResponse);
            });
            return tcs.Task;
        }
    }
}
