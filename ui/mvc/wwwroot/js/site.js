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
            location.href = "/" + path;
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

            $(".upload-error").hide();
            $(".upload-error-2").hide();

            var fileInput = $(formElem).children("input:file");
            if (!fileInput || !fileInput[0] || !fileInput[0].files || !fileInput[0].files[0]) return;
            phideid.ui.showLoadingIndicator();
            var file = fileInput[0].files[0];
            var allowedExtensions = [".pdf", ".csv", ".json", ".xls", ".xlsx", ".doc", ".docx"];
            var fileExtension = file.name.split(".").pop().toLowerCase();

            $(".upload-error").hide();
            if (!allowedExtensions.includes("." + fileExtension)) {
                $(".upload-error").show();
                phideid.ui.hideLoadingIndicator();
                return;
            }

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
                    phideid.ui.showToast("Document uploaded.");
                },
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    $(".upload-error-2").html(`There was an error uploading the document: ${XMLHttpRequest.responseText}`);
                    $(".upload-error-2").show();
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
        
        search() {
            var searchValue = $(".search-row input[type='text']").val();
            sessionStorage.setItem("searchValue", searchValue);
            var query = "?q=" + searchValue;
            location.href = location.pathname + query;
        },

        showToast(message) {
            const toastElem = bootstrap.Toast.getOrCreateInstance(document.getElementById("toast"));
            $(".toast-body").html(message);
            toastElem.show();
        },

        checkInputLength(elem, minLength) {
            if ($(elem).val().length >= minLength) {
                return true;
            }
            return false;
        },

        submitDocumentJustification(id, uri, comment) {
            var formData = {};
            formData.key = id;
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
                    phideid.ui.showToast("Document updated.");
                },
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    phideid.ui.hideLoadingIndicator();
                    phideid.ui.showToast(`There was an error updating the document: ${XMLHttpRequest.responseText}`);
                }

            });
        },

        updateDocumentStatus(id, uri, status) {
            var formData = {};
            formData.key = id;
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
                    phideid.ui.showToast("Document updated.");
                },
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    phideid.ui.hideLoadingIndicator();
                    phideid.ui.showToast(`There was an error updating the document: ${XMLHttpRequest.responseText}`);
                }

            });
        },

        deleteDocument(id, uri, indexOnly) {

            var formData = {};
            formData.key = id;
            formData.uri = uri;

            var endpoint = indexOnly ? '/api/documents/deletefromsearchindex' : '/api/documents/delete';

            phideid.ui.showLoadingIndicator();

            $.ajax({
                url: endpoint,
                type: 'POST',
                data: JSON.stringify(formData),
                contentType: 'application/json',
                success: function (data) {
                    phideid.ui.hideLoadingIndicator();
                    phideid.ui.showToast("Document deleted.");
                },
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    phideid.ui.hideLoadingIndicator();
                    phideid.ui.showToast(`There was an error deleting the document: ${XMLHttpRequest.responseText}`);
                }
                
            });
        },

        downloadFile(filename) {
            window.open("/api/documents/" + filename, "_blank");
        }
    };


})();

$(document).ready(function () {
    phideid.ui.initialize();
    $('.redacted-content-td.tagged>div.content').bind('mousemove', function (e) { phideid.ui.showPIIHover($(this).children('.piiHover'), e); }).bind('mouseout', function () { phideid.ui.hidePIIHover($(this).children('.piiHover')); });
    $('.redacted-content-td.tagged>div.content').bind('click', function () {
        if ($(this).children('.piiHover').hasClass('containsNoPii')) return;
        phideid.ui.showPIIDetails($(this).children('.piiDetails'), $('#piiDialog'), $('#piiEntitiesContent'));
    });
    $('#addTag').bind('click', function () { phideid.ui.addUploadTag($("#uploadTagEntry").val(), $("#uploadTagCloud"), $("#uploadTags")); $("#uploadTagEntry").val("") });
    $('#submitUpload').bind('click', function () { phideid.ui.submitUpload($("#uploadForm")); });
    $(".upload-button").bind('click', function () { phideid.ui.resetUpload($("#uploadForm")); });
    $('#uploadTagEntry').bind('keypress', function (e) { phideid.ui.preventNonAlphaNumericKeys(e); });
    $(".search-row .search-button").bind("click", function () { phideid.ui.search() });
    $(".submit-justification-text").bind("keyup", function (e) { var isValid = phideid.ui.checkInputLength($(this), 3); var btn = $(this).parents(".row").find(".submit-justification-button"); if (isValid) { $(btn).removeAttr("disabled"); } else { $(btn).attr("disabled", "disabled"); } });
    $(".submit-justification-button").bind("click", function () { var id = $(this).parent().attr("data-id"); var comment = $(this).parents(".redacted-content-td").find(".submit-justification-text").val(); var uri = $(this).attr("data-href"); phideid.ui.submitDocumentJustification(id, uri, comment); });
    $(".approve-button").bind("click", function () { var id = $(this).parent().attr("data-id"); var comment = $(this).parents(".redacted-content-td").find(".submit-justification-text").val(); var uri = $(this).attr("data-href"); phideid.ui.updateDocumentStatus(id, uri, 4); });
    $(".deny-button").bind("click", function () { var id = $(this).parent().attr("data-id"); var comment = $(this).parents(".redacted-content-td").find(".submit-justification-text").val(); var uri = $(this).attr("data-href"); phideid.ui.updateDocumentStatus(id, uri, 5); });
    $(".delete-button").bind("click", function () { var id = $(this).parent().attr("data-id"); var uri = $(this).attr("data-href"); phideid.ui.deleteDocument(id, uri); });
    $(".download-button").bind("click", function () { var file = $(this).attr("data-href"); phideid.ui.downloadFile(file); });
});