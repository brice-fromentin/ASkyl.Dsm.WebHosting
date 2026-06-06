// Returns the browser's preferred language (e.g. "en-US", "fr-FR")
navigator.language = {
    get: function () {
        return navigator.language || navigator.userLanguage || navigator.browserLanguage || 'en-US';
    }
};
