var gulp = require("gulp");
var msbuild = require("gulp-msbuild");
var debug = require("gulp-debug");
var foreach = require("gulp-foreach");
var path = require("path");
var nugetRestore = require("gulp-nuget-restore");
var fs = require("fs");
var util = require("gulp-util");
var merge = require("merge-stream");
var runSequence = require("run-sequence");

var config = require("./gulp-config.js")();

module.exports.config = config;

gulp.task('default', function (callback) {
    return runSequence(
        "01-Copy-Sitecore-Dependencies",
        callback);
});

gulp.task("01-Copy-Sitecore-Dependencies", function () {
    fs.statSync(config.sitecoreLibraries);

    var files = config.sitecoreLibraries + "/**/*";

    var libs = gulp.src(files).pipe(gulp.dest('./Libraries/Sitecore'));

    return merge(libs);
});

gulp.task("02-Publish-All-Projects", function (callback) {
    return runSequence(
      "Build-Solution",
      "Publish-Foundation-Projects",
      "Publish-Feature-Projects",
      "Publish-Project-Projects", callback);
});

/*****************************
  Publish
*****************************/
var publishProjects = function (location, dest) {
    dest = dest || config.websiteRoot;
    var targets = ["Build"];

    console.log("publish to " + dest + " folder");
    console.log("location: " + location);
    return gulp.src([location + "/**.csproj"])
      .pipe(foreach(function (stream, file) {
          return stream
            .pipe(debug({ title: "Building project:" }))
            .pipe(msbuild({
                targets: targets,
                configuration: config.buildConfiguration,
                logCommand: false,
                verbosity: "minimal",
                stdout: true,
                errorOnFail: true,
                maxcpucount: 0,
                toolsVersion: 14.0,
                properties: {
                    DeployOnBuild: "true",
                    DeployDefaultTarget: "WebPublish",
                    buildConfiguration: config.buildConfiguration,
                    WebPublishMethod: "FileSystem",
                    DeleteExistingFiles: "false",
                    publishUrl: dest,
                    _FindDependencies: "false"
                }
            }));
      }));
};

gulp.task("Build-Solution", function () {
    var targets = ["Build"];
    if (config.runCleanBuilds) {
        targets = ["Clean", "Build"];
    }
    var solution = "./" + config.solutionName + ".sln";
    return gulp.src(solution)
        .pipe(msbuild({
            targets: targets,
            configuration: config.buildConfiguration,
            logCommand: false,
            verbosity: "minimal",
            stdout: true,
            errorOnFail: true,
            maxcpucount: 0,
            toolsVersion: 14.0
        }));
});

gulp.task("Publish-Foundation-Projects", function () {
    return publishProjects("./Foundation");
});

gulp.task("Publish-Feature-Projects", function () {
    return publishProjects("./Feature");
});

gulp.task("Publish-Project-Projects", function () {
    return publishProjects("./Project");
});
