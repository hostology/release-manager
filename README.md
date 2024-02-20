# Release Manager
A CLI application to simplify release process for multiple repositories.

## Requirements
- Visual Studio / Rider
- .NET 7.0

## Configuration file
Release manager requires configuration file to run.
You can find example of config file in the repository.

    {
        "masterBranch": "main",
        "repositories": [
            {
                "path": "C:\\Users\\someUser\\source\\test-repository"
            }
        ],
        "jira": {
            "url": "https://hostology-platform.atlassian.net/",
            "username": "bialyrobert12@gmail.com",
            "password": "",
            "releasableLabels": [
                "QA_PASSED",
                "NOUAT"
            ],
            "project": {
                "id": "HT",
                "failedMessageTemplate": "Hello <@U05HAH34PS7>!\n\nHere's a list of failed issues in Jira.\n\n{0}",
                "incorrectIssueTemplate": "Issue {0} has status {1} with label {2}.",
                "missingLabelsTemplate": "Issue {0} has status and no labels.",
                "rules": [
                    {
                        "status": "DEV TEST",
                        "labels": [
                            "QA_PASSED",
                            "NOUAT"
                        ]
                    }
                ]
            }
        },
        "slack": {
            "channel": "jira-test",
            "token": ""
        },
        "git": {
            "masterBranch": "master",
            "incrementVersionMessageTemplate": "Updated package.json version to {0}",
            "email": "",
            "token": "",
            "remote": "origin",
            "uatVersionPrefix": "uat/"
        }
    }

### Parameters
#### Git
- masterBranch - the main branch in your repository (eg. main/master/trunk)
- repositories - a list of repositories you would like to release, with their absolute paths
- email - an email used to authenticate to repository
- token - password or token used for validation
- remote - remote server name, default: origin
- uatVersionPrefix - prefix for uat

#### Jira
- url - Jira instance uri
- username - User email
- password - user assigned token. You can read more about it [here](https://support.atlassian.com/atlassian-account/docs/manage-api-tokens-for-your-atlassian-account/).
- releasableLabels - if Jira issue has one of those, it considers issue as releasable

##### Project
- id - Jira project id
- failedMessageTemplate - Beginning of the failed validation message.
- incorrectIssueTemplate - Template message for issues with incorrect labels.
- missingLabelsTemplate - Template message for issues with missing labels.
- rules - Set of rules for a project. 
- rules[].status - Status for which the rule will be applied.
- rules[].labels - Set of acceptable labels for status.

#### Slack
- channel - Slack channel where messages will be published
- token - Application token

## Usage
You can use an application by simply calling it in your terminal.

    ./Hostology.ReleaseManager.exe

To see documentation use it with `-h` or `--help` flag

    ./Hostology.ReleaseManager.exe --help

### Feeding configuration to application
There are two options for application to be feed with configuration.
- You can simply put `config.json` file in the same folder as the application
- You can show where the config file is with a use `-c` or `--configuration-path` parameter

        ./Hostology.ReleaseManager.exe -c ../../example-config.json

        ./Hostology.ReleaseManager.exe --configuration-path ../../example-config.json

### Application options
Use `-n` or `--no-slack` to prevent command line from sending validation message to slack.

    ./Hostology.ReleaseManager.exe --no-slack

Use `-s` or `--skip-validation` to skip project validation.

    ./Hostology.ReleaseManager.exe --skip-validation

Use `-d` or `--dry-run` to prevent command line from releasing found commits.

    ./Hostology.ReleaseManager.exe --dry-run