/*
 * Dashboard tweaks applied via injected script because Xabaril's UI bundle
 * exposes no config knobs and we don't want to fork it. A MutationObserver
 * re-runs the handlers on every re-render.
 *
 * 1. Humanize the DURATION column. The bundle renders raw TimeSpan strings
 *    like "00:00:02.1930578"; rewrite them as "243ms", "2.19s", or "1m23s".
 * 2. Show a campus-status link under the header when any "campus-*" check
 *    is non-healthy, so operators can quickly check status.ucdavis.edu for
 *    a known UCD outage that might explain the failure.
 */
(function () {
    const DURATION_PATTERN = /^\d+:\d+:\d+\.\d+$/;
    const CAMPUS_STATUS_URL = "https://status.ucdavis.edu/";
    const BANNER_ID = "campus-status-banner";

    function formatDuration(text) {
        const m = text.match(/^(\d+):(\d+):(\d+)\.(\d+)$/);
        if (!m) return text;
        const hours = +m[1];
        const mins = +m[2];
        const secs = +m[3];
        // TimeSpan fractional part is 7 digits (100-ns ticks); normalise to ms
        const frac = m[4].padEnd(7, "0").slice(0, 7);
        const fracMs = +frac / 10000;
        const totalMs = (hours * 3600 + mins * 60 + secs) * 1000 + fracMs;

        if (totalMs < 1000) return Math.round(totalMs) + "ms";
        if (totalMs < 60000) return (totalMs / 1000).toFixed(2) + "s";
        // Round to whole seconds first, then split, so 59.6s at the boundary
        // rolls into the next minute instead of rendering as "1m60s".
        const totalSec = Math.round(totalMs / 1000);
        return Math.floor(totalSec / 60) + "m" + (totalSec % 60) + "s";
    }

    function reformatDurations() {
        document.querySelectorAll("td").forEach(function (cell) {
            const t = cell.textContent.trim();
            if (DURATION_PATTERN.test(t)) cell.textContent = formatDuration(t);
        });
    }

    function hasUnhealthyCampusCheck() {
        const rows = document.querySelectorAll("tr");
        for (const row of rows) {
            const nameCell = row.querySelector("td");
            if (!nameCell) continue;
            if (!nameCell.textContent.trim().startsWith("campus-")) continue;
            const icon = row.querySelector(".hc-status .material-icons");
            // check_circle = Healthy; anything else (error, warning, cancel) is a problem
            if (icon && icon.textContent.trim() !== "check_circle") return true;
        }
        return false;
    }

    function updateCampusBanner() {
        const existing = document.getElementById(BANNER_ID);
        if (hasUnhealthyCampusCheck()) {
            if (existing) return;
            const header = document.querySelector(".hc-liveness__header");
            if (!header) return;
            const banner = document.createElement("div");
            banner.id = BANNER_ID;
            const link = document.createElement("a");
            link.href = CAMPUS_STATUS_URL;
            link.target = "_blank";
            link.rel = "noopener";
            link.textContent = "Check UC Davis campus status";
            banner.appendChild(
                document.createTextNode("One or more campus services are reporting issues. "));
            banner.appendChild(link);
            banner.appendChild(document.createTextNode("."));
            header.insertAdjacentElement("afterend", banner);
        } else if (existing) {
            existing.remove();
        }
    }

    function onMutation() {
        reformatDurations();
        updateCampusBanner();
    }

    const obs = new MutationObserver(onMutation);

    function start() {
        onMutation();
        obs.observe(document.body, { childList: true, subtree: true, characterData: true });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", start);
    } else {
        start();
    }
})();
