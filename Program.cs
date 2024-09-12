using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using RestSharp;

class Program
{
    static async Task Main(string[] args)
    {
        // Build configuration
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        IConfiguration config = builder.Build();

        // Get GitLab instance URL and private token from configuration
        string gitlabUrl = config["GitLab:Url"];
        string privateToken = config["GitLab:PrivateToken"];
        int projectId = int.Parse(config["GitLab:ProjectId"]);
        Console.WriteLine($"PROJECT_ID: {projectId}");

        // Debugging statements
        Console.WriteLine($"GITLAB_URL: {gitlabUrl}");
        Console.WriteLine($"GITLAB_PRIVATE_TOKEN: {privateToken}");

        if (string.IsNullOrEmpty(gitlabUrl) || string.IsNullOrEmpty(privateToken))
        {
            Console.WriteLine("Please set the GITLAB_URL and GITLAB_PRIVATE_TOKEN environment variables.");
            return;
        }

        // Display menu
        Console.WriteLine("Select an option:");
        Console.WriteLine("1. Pipeline House Cleaner");
        Console.WriteLine("2. Who has a Pipeline");
        Console.WriteLine("3. Show all pipelines");
        Console.Write("Enter your choice: ");
        string choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                await PipelineHouseCleaner(config, gitlabUrl, privateToken, projectId);
                break;
            case "2":
                await WhoHasAPipeline(gitlabUrl, privateToken);
                break;
            case "3":
                await ShowAllPipelines(config, gitlabUrl, privateToken, projectId);
                break;
            default:
                Console.WriteLine("Invalid choice. Exiting.");
                break;
        }
    }

    static async Task PipelineHouseCleaner(IConfiguration config, string gitlabUrl, string privateToken, int projectId)
    {
        // Get user input for pipelineIdHighLimit and branch name
        Console.Write("Enter the pipeline ID high limit: ");
        int pipelineIdHighLimit = int.Parse(Console.ReadLine());

        Console.Write("Enter the branch name: ");
        string targetBranchName = Console.ReadLine();

        int perPage = 100; // Number of items per page
        int page = 1; // Initial page number
        var client = new RestClient(gitlabUrl); // RestClient for GitLab API
        var allPipelines = new List<dynamic>(); // List to store all retrieved pipelines

        // Loop to retrieve all pipelines using pagination
        while (true)
        {
            var request = new RestRequest($"api/v4/projects/{projectId}/pipelines", Method.Get);
            request.AddHeader("PRIVATE-TOKEN", privateToken);
            request.AddParameter("per_page", perPage);
            request.AddParameter("page", page);

            var response = client.Execute(request);
            if (response.IsSuccessful)
            {
                var pipelines = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response.Content);
                if (pipelines.Count == 0)
                {
                    break; // No more pipelines to retrieve
                }
                allPipelines.AddRange(pipelines);
                page++;
            }
            else
            {
                Console.WriteLine("Failed to retrieve pipelines");
                break;
            }
        }

        // Display retrieved pipeline details
        foreach (var pipeline in allPipelines)
        {
            Console.WriteLine($"Pipeline ID: {pipeline.id}");
            Console.WriteLine($"Status: {pipeline.status}");
            Console.WriteLine($"Ref: {pipeline["ref"]}");
            Console.WriteLine($"Created At: {pipeline.created_at}");
            Console.WriteLine();
        }

        // Loop to delete pipelines based on user-defined criteria
        foreach (var pipeline in allPipelines)
        {
            string pipelineRef = (string)pipeline["ref"]; // ref is the branch name
            int pipelineId = pipeline.id;
            bool isTargetRef = pipelineRef == targetBranchName;
            bool isPipelineIdLessThanThreshold = pipelineId < pipelineIdHighLimit;

            try
            {
                Console.WriteLine($"Executing pipeline: {pipelineId}");

                if (isTargetRef && isPipelineIdLessThanThreshold)
                {
                    var deleteRequest = new RestRequest($"api/v4/projects/{projectId}/pipelines/{pipelineId}", Method.Delete);
                    deleteRequest.AddHeader("PRIVATE-TOKEN", privateToken);
                    var deleteResponse = client.Execute(deleteRequest);
                    if (deleteResponse.IsSuccessful)
                    {
                        Console.WriteLine($"Deleted pipeline {pipelineId}");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to delete pipeline {pipelineId}");
                    }
                }
                else
                {
                    Console.WriteLine($"Outside requested range or wrong branch. Skipping pipeline {pipelineId}");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete pipeline {pipelineId}");
                Console.WriteLine(ex.Message);
            }
        }
    }

    static async Task ShowAllPipelines(IConfiguration config, string gitlabUrl, string privateToken, int projectId)
    {
        int perPage = 100; // Number of items per page
        int page = 1; // Initial page number
        var client = new RestClient(gitlabUrl); // RestClient for GitLab API
        var allPipelines = new List<dynamic>(); // List to store all retrieved pipelines

        // Loop to retrieve all pipelines using pagination
        while (true)
        {
            var request = new RestRequest($"api/v4/projects/{projectId}/pipelines", Method.Get);
            request.AddHeader("PRIVATE-TOKEN", privateToken);
            request.AddParameter("per_page", perPage);
            request.AddParameter("page", page);

            var response = client.Execute(request);
            if (response.IsSuccessful)
            {
                var pipelines = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response.Content);
                if (pipelines.Count == 0)
                {
                    break; // No more pipelines to retrieve
                }
                allPipelines.AddRange(pipelines);
                page++;
            }
            else
            {
                Console.WriteLine("Failed to retrieve pipelines");
                break;
            }
        }

        // Display retrieved pipeline details
        foreach (var pipeline in allPipelines)
        {
            Console.WriteLine($"Pipeline ID: {pipeline.id}");
            Console.WriteLine($"Status: {pipeline.status}");
            Console.WriteLine($"Ref: {pipeline["ref"]}");
            Console.WriteLine($"Created At: {pipeline.created_at}");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Retrieves the list of projects and displays the names of projects that have active pipelines.
    /// </summary>
    /// <param name="gitlabUrl">The URL of the GitLab instance.</param>
    /// <param name="privateToken">The private token for authentication.</param>
    static async Task WhoHasAPipeline(string gitlabUrl, string privateToken)
    {
        var projects = await GetProjectsAsync(gitlabUrl, privateToken);
        var activeProjects = new List<string>();

        foreach (var project in projects)
        {
            var projectId = (int)project["id"];
            var projectName = (string)project["name"];
            var pipelines = await GetActivePipelinesAsync(projectId, projectName, gitlabUrl, privateToken);
            if (pipelines.Count > 0)
            {
                activeProjects.Add(projectName);
            }
        }


        if (activeProjects.Count > 0)
        {
            Console.WriteLine("Projects with active pipelines:");
            foreach (var projectName in activeProjects)
            {
                Console.WriteLine($"  - {projectName}");
            }
        }
        else
        {
            Console.WriteLine("No projects with active pipelines found.");
        }
    }

    private static async Task<JArray> GetProjectsAsync(string gitlabUrl, string privateToken)
    {
        var allProjects = new JArray();
        int page = 1;
        int perPage = 100; // Number of items per page

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", privateToken);

            while (true)
            {
                var response = await client.GetStringAsync($"{gitlabUrl}/api/v4/projects?per_page={perPage}&page={page}");
                var projects = JArray.Parse(response);

                if (projects.Count == 0)
                {
                    break; // No more projects to retrieve
                }

                allProjects.Merge(projects);
                page++;
            }
        }

        return allProjects;

        //using (var client = new HttpClient())
        //{
        //    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", privateToken);
        //    var response = await client.GetStringAsync($"{gitlabUrl}/api/v4/projects");
        //    return JArray.Parse(response);
        //}
    }

    private static async Task<JArray> GetActivePipelinesAsync(int projectId, string projectName, string gitlabUrl, string privateToken)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", privateToken);
            try
            {
                var response = await client.GetStringAsync($"{gitlabUrl}/api/v4/projects/{projectId}/pipelines");
                return JArray.Parse(response);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                Console.WriteLine($"Access forbidden for project ID: {projectId}, Name: {projectName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving pipelines for project ID: {projectId}, Name: {projectName}");
                Console.WriteLine(ex.Message);
            }
        }
        return new JArray(); // Return an empty JArray in case of an error
    }
}
