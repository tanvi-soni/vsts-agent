using System;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.Agent.Util;

using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts
{
    [ServiceLocator(Default = typeof(GitHubClient))]
    public interface IGitHubClient : IAgentService
    {
        GitHubRepository GetUserRepo(string accessToken, string repository);
    }

    public class GitHubClient : AgentService, IGitHubClient
    {
        private const string GithubRepoUrlFormat = "https://api.github.com/repos/{0}";

        public GitHubRepository GetUserRepo(string accessToken, string repositoryName)
        {
            string errorMessage;
            string url = StringUtil.Format(GithubRepoUrlFormat, repositoryName);
            var repository = QueryItem<GitHubRepository>(accessToken, url, out errorMessage);

            if (errorMessage != null)
            {
                throw new InvalidOperationException(errorMessage);
            }

            return repository;
        }

        private static T QueryItem<T>(string accessToken, string url, out string errorMessage)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            AddDefaultRequestHeaders(request, accessToken);
            return SendRequestForSingleItem<T>(request, out errorMessage);
        }

        private static void AddDefaultRequestHeaders(HttpRequestMessage request, string accessToken)
        {
            request.Headers.Add("Accept", "application/vnd.GitHubData.V3+json");
            request.Headers.Add("Authorization", "Token " + accessToken);
            request.Headers.Add("User-Agent", "VSTS-Agent/" + Constants.Agent.Version);
        }

        private static T SendRequestForSingleItem<T>(HttpRequestMessage request, out string errorMessage)
        {
            using (var httpClientHandler = new HttpClientHandler())
            using (var httpClient = new HttpClient(httpClientHandler) { Timeout = new TimeSpan(0, 0, 30) })
            {
                Task<HttpResponseMessage> sendAsyncTask = httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                sendAsyncTask.Wait();

                HttpResponseMessage response = sendAsyncTask.Result;
                if (!response.IsSuccessStatusCode)
                {
                    errorMessage = response.StatusCode.ToString();
                    return default(T);
                }

                Task<string> result = response.Content.ReadAsStringAsync();
                result.Wait();

                errorMessage = null;
                return JsonConvert.DeserializeObject<T>(result.Result);
            }
        }
    }

    [DataContract]
    public class GitHubRepository
    {
        [DataMember(EmitDefaultValue = false)]
        public int? Id { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Clone_url { get; set; }
   }
}