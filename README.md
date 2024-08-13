# GitLab Pipeline Management Tool
This application allows you to manage GitLab pipelines by retrieving and optionally deleting pipelines based on user-defined criteria.

Added functionality to find all projects with a pipeline

### Prerequisites
- .NET SDK
- RestSharp library
- Newtonsoft.Json library

- ## Setup
1. Clone the repository.
2. Open the project in Visual Studio.
3. Restore the required NuGet packages.

Before running this application, ensure you have the following:

- A GitLab account with access to the desired project.
- A GitLab personal access token (private token) with appropriate permissions to read pipelines and delete them.

### Configuration
1. Create an `appsettings.json` file in the root of your project with the following content:

```
{
  "GitLab": {
    "Url": "https://gitlab.example.com",
    "PrivateToken": "your_private_token",
    "ProjectId": 123
  }
}
```

### Usage
1. Clone this repository to your local machine.
2. Open the `Program.cs` file in your preferred C# development environment.
3. Replace the following placeholders with your actual values:
    - `gitlabUrl`: The base URL of your GitLab instance (e.g., `https://git.thevillages.com/`).
    - `privateToken`: Your GitLab personal access token.
    - `projectId`: The ID of the GitLab project (e.g., `271` for `capacitor-plugin-arcmap`).
    - Adjust any other parameters as needed (e.g., `perPage`, `page`, etc.).
4. Build and run the application.
5. Enter the pipeline ID high limit when prompted.
6. Enter the branch name when prompted.

The application will retrieve all pipelines and display their details. It will then delete pipelines that match the specified branch name and have an ID less than the specified high limit.

### Functionality
1. Retrieves all pipelines for the specified project.
2. Displays pipeline information, including ID, status, reference (branch/tag), and creation timestamp.
3. Deletes pipelines that meet specific criteria (e.g., target reference and pipeline ID threshold).

### Example
- Enter the pipeline ID high limit: 5790 Enter the branch name: CNP34-gettingJavaTestsToWork
- Pipeline ID: 5789 Status: success Ref: CNP34-gettingJavaTestsToWork Created At: 2023-10-01T12:34:56Z
- Executing pipeline: 5789 Deleted pipeline 5789
- Pipeline ID: 5791 Status: failed Ref: CNP34-gettingJavaTestsToWork Created At: 2023-10-02T12:34:56Z
- Executing pipeline: 5791 Outside requested range or wrong branch. Skipping pipeline 5791

### Notes
- Be cautious when deleting pipelines, as this action is irreversible.
- Customize the logic in the second loop to match your specific requirements for pipeline deletion.
- The application uses pagination to retrieve all pipelines. Adjust the `perPage` variable in `Program.cs` if needed.
- Ensure your private token has the necessary permissions to access and delete pipelines in the specified project.

### Screenshots

![Screenshot 1](https://github.com/jbphillips/RemovePipelineGitLab/blob/develop/images/consoleApp1.png?raw=true)
*User Interaction*

![Screenshot 2](https://github.com/jbphillips/RemovePipelineGitLab/blob/develop/images/consoleApp2.png?raw=true)
*Program.cs filtering exceptable criteria*