window.blazorInterop = {
    scrollToElement: function (element) {
        if (element) {
            element.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    }
};