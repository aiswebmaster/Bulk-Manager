define(["sitecore", "jquery"], function (Sitecore, $) {
    var ImportPage = Sitecore.Definitions.App.extend({
        initialized: function () {
            this.on("import:action", this.importAction, this);
        },
        importAction: function () {
            PassImportData(this);
        }
    });

    function PassImportData(app) {

        var formData = new FormData();

        formData.append("file", $('input[type=file]')[0].files[0]);
        formData.append("versionChecked", app.CreateNewVersion.attributes.isChecked);
        formData.append("database", app.DatabaseTextbox.attributes.text);

        $.ajax({
            url: '/api/Sitecore/Import/ImportItems',
            type: 'POST',
            data: formData,
            context: this,
            processData: false,
            contentType: false,
            cache: false,
            dataType: 'json',
            success: function (data) {
                if (data && data.ItemResults.length > 0) {
                    var output = "";
                    var d = new Date();
                    output = output + d.toISOString() + ' - Created Items: ' + data.ItemsCreated;
                    output = output + '\n' + d.toISOString() + ' - Updated Items: ' + data.ItemsUpdated;
                    output = output + '\n' + d.toISOString() + ' - Failed Items: ' + data.ItemsFailed;
                    
                    // Loop thru collection
                    for (var item in data.ItemResults) {
                        output = output + '\n' + d.toISOString() + ' - Item Details: ' + JSON.stringify(data.ItemResults[item]);
                    }

                    output = output + '\n \n';
                    app.TextAreaResults.set('text', output);
                } else {
                    app.TextAreaResults.set('text', 'No Data Imported');
                }
            },
            error: function (data) {
                app.TextAreaResults.set('text', 'An error occured during import.');
            }
        });
    }



    return ImportPage;
});