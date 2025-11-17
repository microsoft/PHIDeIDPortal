const phideid = {};

phideid.ui = (function () {

    var selectedSearchElem = "prefix";

    return {

        initialize() {
            var path = sessionStorage.getItem("path");
            if (!path) path = "index";
            $(".tab").each(function () {
                var elemPath = $(this).attr('data-href');
                if (elemPath === path) {
                    $(this).children().removeClass("unhighlight");
                    $(this).children().addClass("highlight");
                }
                else {
                    $(this).children().addClass("unhighlight");
                    $(this).children().removeClass("highlight");
                }

                $(this).children('div').children('div').bind('click', function () {
                    phideid.ui.redirect(elemPath);
                });
            });

            phideid.ui.setSearchBoxValue();
            phideid.ui.setViewAllIcon();
        },

        showPIIHover(elem, e) {
            elem.css('top', e.pageY + 5).css('left', e.pageX + 5).show();
        },

        hidePIIHover(elem) {
            elem.hide();
        },

        showPIIDetails(elem, modalElem, modalContentElem) {
            modalContentElem.val(elem.html());
            modalElem.modal('show');
        },

        redirect(path) {
            sessionStorage.setItem("path", path);
            phideid.ui.search("/" + path);
        },

        addUploadTag(tagValue, tagCloudElem, tagInputElem) {
            if (tagValue.length === 0) return;
            var $tagElem = $("<div>", { "class": "tagCloudElem" })
                .append("<span>" + tagValue + "</span><span><i class='bi bi-x-lg'></i></span>")
                .bind("click", function () { phideid.ui.removeUploadTag($(this), $("#uploadTagCloud"), $("#uploadTags")); });

            tagCloudElem.append($tagElem);
        },

        removeUploadTag(tagElem) {
            tagElem.remove();
        },

        resetUpload(formElem) {
            $(formElem).children("input:file").val(null);
            $(".upload-error").hide();
            $(".upload-error-2").hide();
            $("#uploadTagEntry").val(null);
            var tagCloud = $(formElem).children("#uploadTagCloud");
            tagCloud.children().each(function () {
                phideid.ui.removeUploadTag(this);
            });

        },

        submitUpload(formElem) {
            var fileInput = $(formElem).children("input:file");
            if (!fileInput || !fileInput[0] || !fileInput[0].files || !fileInput[0].files[0]) return;
            phideid.ui.showLoadingIndicator();
            var file = fileInput[0].files[0];

            var tagCloud = $(formElem).children("#uploadTagCloud");
            var tagInput = $(formElem).children("#uploadTags");

            var tagCount = 0;
            var tagString = "";

            tagCloud.children().each(function () {

                tagString += (tagCount === 0) ? $(this).children('span').first().html() : "," + $(this).children('span').first().html();
                tagCount++;

            });

            tagInput.val(tagString);

            var formData = new FormData();
            formData.append('file', fileInput[0].files[0]);
            formData.append('uploadTags', tagString)

            $.ajax({
                url: '/api/documents/upload',
                type: 'POST',
                data: formData,
                processData: false,
                contentType: false,
                success: function (data) {
                    $(".loading").hide();
                    $("#uploadDialog").modal('hide');
                    phideid.ui.showToast("Document uploaded.", false, true);
                },
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    phideid.ui.showToast(`Error uploading: ${XMLHttpRequest.responseText}`, true, false);
                    phideid.ui.hideLoadingIndicator();
                }

            });

        },

        showLoadingIndicator() {
            $(".loading").show();
        },

        hideLoadingIndicator() {
            $(".loading").hide();
        },

        preventNonAlphaNumericKeys(e) {
            var keyCode = e.keyCode || e.which;
            var regex = /^[A-Za-z0-9]+$/;
            var isValid = regex.test(String.fromCharCode(keyCode));

            if (!isValid) {
                e.preventDefault();
            }
        },

        setSearchBoxValue(value) {
            if (!value) {
                var searchValue = sessionStorage.getItem("searchValue");
                if (!searchValue) return;
                value = searchValue;
            }
            $(".search-row input[type='text']").val(value);
        },

        search(path) {
            var searchValue = $(".search-row input[type='text']").val();
            sessionStorage.setItem("searchValue", searchValue);
            var query = (searchValue && searchValue.length > 0) ? "q=" + searchValue : "";
            var viewAll = sessionStorage.getItem("viewMe");
            var view = (viewAll && viewAll === "me") ? "&v=" + sessionStorage.getItem("viewMe") : "";
            var params = (query || view) ? "?" : "";
            var destination = (path) ? path : location.pathname;
            location.href = destination + params + query + view;
        },

        toggleViewAll() {
            var viewAll = sessionStorage.getItem("viewMe");
            if (viewAll && viewAll === "me") { sessionStorage.removeItem("viewMe"); }
            else { sessionStorage.setItem("viewMe", "me"); }
            phideid.ui.setViewAllIcon();
            phideid.ui.search();
        },

        setViewAllIcon() { 
            var viewAll = sessionStorage.getItem("viewMe");
            if (viewAll && viewAll === "me") { $(".viewall-button").html("<i class='bi bi-person-lines-fill'></i>"); }
            else { $(".viewall-button").html("<i class='bi bi-people-fill'></i>"); }
        },

        showToast(message,isError,showReload) {
            const toastElem = bootstrap.Toast.getOrCreateInstance(document.getElementById("toast"));
            if (showReload) { message += "&nbsp;<a id='toastreload' href='javascript:phideid.ui.toastReload();'>Reload</a>"; }
            if (isError) { message = "<i class='bi bi-exclamation-triangle'></i> " + message; }
            else { message = "<i class='bi bi-info-circle'></i> " + message; }
            
            $(".toast-body").html(message);
            toastElem.show();
        },

        toastReload() {
            $("#toastreload").attr('href', '#');
            $("#toastreload").html("Reloading in 5 sec ...");
            var count = 0;
            var interval = setInterval(function () {
                $("#toastreload").html(`Reloading in ${(4 - count++)} sec ...`);
                if (count >= 5) {
                    clearInterval(interval);
                    location.reload();
                }
            },1000);
        },

        checkInputLength(elem, minLength) {
            if ($(elem).val().length >= minLength) {
                return true;
            }
            return false;
        },

        submitDocumentJustification(uri, comment) {
            var formData = {};
            formData.uri = uri;
            formData.comment = comment;

            phideid.ui.showLoadingIndicator();

            $.ajax({
                url: '/api/documents/justify',
                type: 'POST',
                data: JSON.stringify(formData),
                contentType: 'application/json',
                success: function (data) {
                    phideid.ui.hideLoadingIndicator();
                    phideid.ui.showToast("Document updated.",false,false);
                    $(`button[data-href='${uri}']`).closest("tr").remove();
                },
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    phideid.ui.hideLoadingIndicator();
                    var reload = (XMLHttpRequest.status === 409);
                    phideid.ui.showToast(`Error updating the document: ${XMLHttpRequest.responseText}`,true,reload);
                    phideid.ui.disableButtonGroup($(`button[data-href='${uri}']`));
                }

            });
        },

        updateDocumentStatus(uri, status) {
            var formData = {};
            formData.Uri = uri;
            
            var url = '';

            if (status === 4) {
                url = '/api/documents/approve';
            }
            else if (status === 5) {
                url = '/api/documents/deny';
            }

            phideid.ui.showLoadingIndicator();

            $.ajax({
                url: url,
                type: 'POST',
                data: JSON.stringify(formData),
                contentType: 'application/json',
                success: function (data) {
                    phideid.ui.hideLoadingIndicator();
                    phideid.ui.showToast("Document updated.",false,false);
                    phideid.ui.disableButtonGroup($(`button[data-href='${uri}']`));
                    $(`button[data-href='${uri}']`).closest("tr").remove();
                },
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    phideid.ui.hideLoadingIndicator();
                    var reload = (XMLHttpRequest.status === 409);
                    phideid.ui.showToast(`There was an error updating the document: ${XMLHttpRequest.responseText}`,true,reload);
                    phideid.ui.disableButtonGroup($(`button[data-href='${uri}']`));
                }

            });
        },

        async deleteDocument(uri) {
            var formData = {};
            formData.uri = uri;

            var endpoint = '/api/documents/delete';

            phideid.ui.showLoadingIndicator();

            try {
                let response = await $.ajax({
                    url: endpoint,
                    type: 'POST',
                    data: JSON.stringify(formData),
                    contentType: 'application/json'
                });
                phideid.ui.hideLoadingIndicator();
                phideid.ui.showToast("Document deleted.", false, false);
                phideid.ui.disableButtonGroup($(`button[data-href='${uri}']`));

                setTimeout(function () {
                    location.reload();
                }, 3000);
            } catch (error) {
                phideid.ui.hideLoadingIndicator();
                phideid.ui.showToast(`There was an error deleting the document: ${error.responseText}`, true, false);
                phideid.ui.disableButtonGroup($(`button[data-href='${uri}']`));
            }
        },

        reindex() {
            phideid.ui.showLoadingIndicator();

            $.ajax({
                url: '/api/documents/reindex',
                type: 'POST',
                success: function (data) {
                    phideid.ui.hideLoadingIndicator();
                    phideid.ui.showToast("Documents reindexed.",false,false);
                },
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    phideid.ui.hideLoadingIndicator();
                    phideid.ui.showToast(`There was an error reindexing the documents: ${XMLHttpRequest.responseText}`,true,false);
                }

            });
        },

        disableButtonGroup(elem) {
            $(this).attr("disabled", "disabled");
        },

        downloadFile(filename) {
            window.open("/api/documents/" + filename, "_blank");
        }
    };


})();

$(document).ready(function () {
    // Trigger search on Enter key in search input
    $(".search-row input[type='text']").on("keydown", function (e) {
        if (e.key === "Enter") {
            e.preventDefault();
            $(this).closest(".search-row").find(".search-button").click();
        }
    });

    phideid.ui.initialize();
    $('.redacted-content-td.tagged>div.content').bind('mousemove', function (e) { phideid.ui.showPIIHover($(this).children('.piiHover'), e); }).bind('mouseout', function () { phideid.ui.hidePIIHover($(this).children('.piiHover')); });
    $('.redacted-content-td.tagged>div.content').bind('click', function () {
        if ($(this).children('.piiHover').hasClass('containsNoPii')) return;
        phideid.ui.showPIIDetails($(this).children('.piiDetails'), $('#piiDialog'), $('#piiEntitiesContent'));
    });
    $('#addTag').bind('click', function () { phideid.ui.addUploadTag($("#uploadTagEntry").val(), $("#uploadTagCloud"), $("#uploadTags")); $("#uploadTagEntry").val("") });

    $('#submitUpload').bind('click', function () {
        phideid.ui.submitUpload($("#uploadForm"));
    });

    $(".upload-button").bind('click', function () {
        phideid.ui.resetUpload($("#uploadForm"));
    });

    $('#uploadTagEntry').bind('keypress', function (e) { phideid.ui.preventNonAlphaNumericKeys(e); });
    $(".search-row .search-button").bind("click", function () { phideid.ui.search() });
    $(".submit-justification-text").bind("keyup", function (e) { var isValid = phideid.ui.checkInputLength($(this), 3); var btn = $(this).parents(".row").find(".submit-justification-button"); if (isValid) { $(btn).removeAttr("disabled"); } else { $(btn).attr("disabled", "disabled"); } });
    $(".submit-justification-button").bind("click", async function () {
        var comment = $(this).parents(".redacted-content-td").find(".submit-justification-text").val();
        var uri = $(this).attr("data-href");
        await phideid.ui.submitDocumentJustification(uri, comment);        
    });

    $(".approve-button").bind("click", async function () {
        var comment = $(this).parents(".redacted-content-td").find(".submit-justification-text").val();
        var uri = $(this).attr("data-href");
        await phideid.ui.updateDocumentStatus(uri, 4);
    });

    $(".deny-button").bind("click", async function () {
        var comment = $(this).parents(".redacted-content-td").find(".submit-justification-text").val();
        var uri = $(this).attr("data-href");
        await phideid.ui.updateDocumentStatus(uri, 5);
    });
    $(".delete-button").bind("click", async function () {
        var id = $(this).parent().attr("data-id");
        var uri = $(this).attr("data-href");
        await phideid.ui.deleteDocument(uri);    
    });

    $(".download-button").bind("click", function () { var file = $(this).attr("data-href"); phideid.ui.downloadFile(file); });
    $(".viewall-button").bind("click", function () { phideid.ui.toggleViewAll(); });

    // Show/hide clear-search-button based on 'q' querystring value
    function getQueryParam(name) {
        const urlParams = new URLSearchParams(window.location.search);
        return urlParams.get(name);
    }
    if (!getQueryParam('q')) {
        $(".clear-search-button").hide();
    } else {
        $(".clear-search-button").show();
    }

    // Clear search button logic
    $(document).on("click", ".clear-search-button", function () {
        var $input = $(this).closest(".input-group").find("input[type='text']");
        $input.val("");
        sessionStorage.removeItem("searchValue");
        phideid.ui.search();
    });
});