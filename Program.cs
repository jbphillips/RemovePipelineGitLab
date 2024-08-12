using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using RestSharp;

class Program
{
    static void Main(string[] args)
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
}
