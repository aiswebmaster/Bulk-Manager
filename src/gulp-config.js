module.exports = function () {
    var sitecoreRoot = "C:\\Sitecore\\hackathon-822";
    var config = {
        websiteRoot: sitecoreRoot + "\\Website",
        sitecoreLibraries: sitecoreRoot + "\\Website\\bin",
        solutionName: "Sitecore.Hackathon",
        licensePath: sitecoreRoot + "\\Data\\license.xml",
        runCleanBuilds: false,
        buildConfiguration: "Release"
    };
    return config;
}