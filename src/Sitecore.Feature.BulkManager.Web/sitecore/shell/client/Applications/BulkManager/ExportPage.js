define(["sitecore", "jquery"], function (Sitecore, $) {
    var ExportPage = Sitecore.Definitions.App.extend({
        initialized: function () {
            this.on("export:action", this.exportAction, this);
            this.on("export:reset", this.reset, this);
        },
        exportAction: function () {

            // Start Export Action
            var databaseQuery = this.TextboxDatasource.attributes.text;
            var model = {
                databaseName: this.TextBoxDatabase.attributes.text,
                languages: this.TextBoxLanguages.attributes.text,
                includeStandardFields: this.IncludeStandardFields.attributes.isChecked
            };

            if (databaseQuery) {
                model.query = databaseQuery;

                ExportQuery(model);
            } else {
                model.path = this.ParentTreeView.attributes.selectedItemPath;

                ExportPath(model);
            }
        },
        reset: function () {

            // Clear Code
            this.TextBoxDatabase.set('text', '');
            this.TextBoxLanguages.set('text', '');
            this.ParentTreeView.set('selectedItemPath', '');
            this.includeStandardFields.set('isChecked', false);

        }
    });

    function ExportPath(model) {
        window.location.href = "/api/Sitecore/Export/ExportPath?databaseName=" + model.databaseName + "&path=" + model.path + "&languages=" + model.languages + "&includeStandardFields=" + model.includeStandardFields;
    }

    function ExportQuery(model) {
        window.location.href = "/api/Sitecore/Export/ExportQuery?databaseName=" + model.databaseName + "&query=" + model.query + "&languages=" + model.languages + "&includeStandardFields=" + model.includeStandardFields;
    }

    return ExportPage;
});