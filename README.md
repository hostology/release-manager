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
        ]
    }

### Parameters
- masterBranch - the main branch in your repository (eg. main/master/trunk)
- repositories - a list of repositories you would like to release, with their absolute paths

## Usage
You can use an application by simply calling it in your terminal.

    ./Hostology.ReleaseManager.exe

To see documentation use it with `-h` or `--help flag`

    ./Hostology.ReleaseManager.exe --help

### Feeding configuration to application
There are two options for application to be feed with configuration.
- You can simply put `config.json` file in the same folder as the application
- You can show where the config file is with a use `-c` or `--configuration-path` parameter

        ./Hostology.ReleaseManager.exe -c ../../example-config.json

        ./Hostology.ReleaseManager.exe --configuration-path ../../example-config.json