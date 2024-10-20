# DeviousLogger

This is a utility tool for [Devious Client](https://github.com/jbx5/devious-client) used with plugin [TScripts](https://github.com/Tonic-Box/TScriptsRepository).
As TScripts can output menuActions and packets into trade channel of osrs chat window this program basicaly redirects the entire output of devious client with --debug args into a pipeline wich filters menuActions and packets into separate logs.

# Usage

1. Build the solution, you can just use dotnet cli. Just run ```dotnet build``` at folder where the .csproj is located.

2. Copy the build output into folder which contains devious-client-launcher.jar

3. Launch the program, as soon as redirection filter fires log is created or appended.

## Known issues

1. Since this relies on the existence of chat window trade channel, if no such channel exist/isInitialized then no recording of packets/menuActions happens.
   For example landing page after login knows nothing of chat window interface and yet its continuation is a packet or menuAction.
