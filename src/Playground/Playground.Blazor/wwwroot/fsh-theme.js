window.FshTheme = {
    setFavicon: function (faviconUrl) {
        if (!faviconUrl) {
            // Reset to default
            const link = document.querySelector("link[rel~='icon']");
            if (link) {
                link.href = 'favicon.ico?t=' + Date.now();
            }
            return;
        }

        let link = document.querySelector("link[rel~='icon']");
        if (!link) {
            link = document.createElement('link');
            link.rel = 'icon';
            document.head.appendChild(link);
        }
        link.href = faviconUrl + '?t=' + Date.now();
    }
};