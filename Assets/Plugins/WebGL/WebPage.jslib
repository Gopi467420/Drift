mergeInto(LibraryManager.library, {
    // Called by WebPage.Quit in the web build. Refreshes the page without starting the game.
    ReloadPage: function () {
        // Inside another site, like itch.io: reload that whole page, which shows its own Run button.
        // A browser refuses this when the site is on another domain, so it falls through.
        try {
            if (window.top !== window.self) {
                window.top.location.reload();
                return;
            }
        } catch (e) {}

        // Our own page: the flag makes it come back with a Play button instead of loading the game.
        try { sessionStorage.setItem("drift-quit", "1"); } catch (e) {}
        window.location.reload();
    }
});
