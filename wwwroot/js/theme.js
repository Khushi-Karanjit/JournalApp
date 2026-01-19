window.theme = {
    setTheme: function (theme) {
        document.documentElement.dataset.theme = theme;
        localStorage.setItem("theme", theme);
    }
};
