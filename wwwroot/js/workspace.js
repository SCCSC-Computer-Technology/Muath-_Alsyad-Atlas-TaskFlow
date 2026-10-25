
// App #1 - Atlas TaskFlow - Wireframes UI Interactions

document.addEventListener("DOMContentLoaded", function () {

    // ==========================================
    // 1. TEAM TASKS FILTERS
    // ==========================================

    const teamRows =
        document.querySelectorAll("[data-team-row]");

    if (teamRows.length > 0) {

        const search = document.getElementById("teamSearch");
        const project = document.getElementById("teamProject");
        const member = document.getElementById("teamMember");
        const priority = document.getElementById("teamPriority");
        const status = document.getElementById("teamStatus");

        const tabs =
            document.querySelectorAll("[data-team-tab]");

        let activeTab = "All";

        function filterTeamTasks() {

            let visibleCount = 0;

            teamRows.forEach(function (row) {

                const matchesSearch =
                    row.textContent.toLowerCase().includes(
                        search.value.trim().toLowerCase()
                    );

                const matchesProject =
                    !project.value ||
                    row.dataset.project === project.value;

                const matchesMember =
                    !member.value ||
                    row.dataset.member === member.value;

                const matchesPriority =
                    !priority.value ||
                    row.dataset.priority === priority.value;

                const matchesStatus =
                    !status.value ||
                    row.dataset.status === status.value;

                const matchesTab =
                    activeTab === "All" ||
                    row.dataset.status === activeTab;

                const show =
                    matchesSearch &&
                    matchesProject &&
                    matchesMember &&
                    matchesPriority &&
                    matchesStatus &&
                    matchesTab;

                row.hidden = !show;

                if (show) {
                    visibleCount++;
                }

            });

            const empty = document.getElementById("teamEmpty");

            if (empty) {
                empty.hidden = visibleCount !== 0;
            }
        }

        [search, project, member, priority, status]
            .forEach(function (control) {
                control.addEventListener("input", filterTeamTasks);
            });

        tabs.forEach(function (tab) {

            tab.addEventListener("click", function () {

                activeTab = tab.dataset.teamTab;

                tabs.forEach(function (other) {
                    other.classList.remove("active");
                });

                tab.classList.add("active");

                filterTeamTasks();
            });

        });

    }


    // ==========================================
    // 2. INTERACTIVE CALENDAR
    // ==========================================

    const calendarGrid =
        document.getElementById("calendarGrid");

    if (calendarGrid) {

        let shownMonth = new Date();
        shownMonth.setDate(1);

        let selectedDate = null;

        // Sample data only - not read from SQL Server.
        const events = [
            {
                date: "2026-10-20",
                title: "Write project proposal",
                type: "task"
            },
            {
                date: "2026-10-22",
                title: "Research user needs",
                type: "task"
            },
            {
                date: "2026-10-24",
                title: "Create wireframes",
                type: "deadline"
            },
            {
                date: "2026-10-26",
                title: "Design homepage",
                type: "team"
            },
            {
                date: "2026-10-28",
                title: "Review project design",
                type: "task"
            },
            {
                date: "2026-10-29",
                title: "Team meeting",
                type: "event"
            },
            {
                date: "2026-11-02",
                title: "Prepare presentation",
                type: "deadline"
            }
        ];

        function dateKey(date) {

            const year = date.getFullYear();

            const month = String(
                date.getMonth() + 1
            ).padStart(2, "0");

            const day = String(
                date.getDate()
            ).padStart(2, "0");

            return year + "-" + month + "-" + day;
        }

        function visibleEvents(key) {

            return events.filter(function (item) {

                const checkbox = document.querySelector(
                    '[data-cal-type="' + item.type + '"]'
                );

                return item.date === key &&
                    (!checkbox || checkbox.checked);
            });
        }

        function showDayDetails(key) {

            const label =
                document.getElementById("calendarDayLabel");

            const details =
                document.getElementById("calendarDayDetails");

            if (label) {
                label.textContent = key;
            }

            const items = visibleEvents(key);

            if (details) {
                details.textContent = items.length === 0
                    ? "No sample events for this day."
                    : items.map(item => item.title).join(" | ");
            }
        }

        function renderCalendar() {

            const year = shownMonth.getFullYear();
            const month = shownMonth.getMonth();

            const monthLabel =
                document.getElementById("calendarMonthLabel");

            monthLabel.textContent =
                shownMonth.toLocaleDateString("en-US", {
                    month: "long",
                    year: "numeric"
                });

            calendarGrid.replaceChildren();

            const firstDay =
                new Date(year, month, 1).getDay();

            const daysInMonth =
                new Date(year, month + 1, 0).getDate();

            const totalCells =
                Math.ceil((firstDay + daysInMonth) / 7) * 7;

            const todayKey = dateKey(new Date());

            for (let index = 0; index < totalCells; index++) {

                const date = new Date(
                    year,
                    month,
                    index - firstDay + 1
                );

                const key = dateKey(date);

                const cell = document.createElement("button");
                cell.type = "button";
                cell.className = "wf-calendar-day";

                if (date.getMonth() !== month) {
                    cell.classList.add("outside");
                }

                if (key === todayKey) {
                    cell.classList.add("today");
                }

                if (key === selectedDate) {
                    cell.classList.add("selected");
                }

                const number = document.createElement("span");
                number.className = "wf-calendar-number";
                number.textContent = date.getDate();

                cell.appendChild(number);

                const items = visibleEvents(key);

                items.slice(0, 2).forEach(function (item) {

                    const event = document.createElement("span");

                    event.className =
                        "wf-calendar-event " + item.type;

                    event.textContent = item.title;

                    cell.appendChild(event);

                });

                cell.addEventListener("click", function () {

                    selectedDate = key;

                    showDayDetails(key);
                    renderCalendar();

                    document.querySelector(
                        '[data-calendar-view-btn="day"]'
                    ).click();

                });

                calendarGrid.appendChild(cell);

            }
        }

        document.querySelectorAll("[data-calendar-shift]")
            .forEach(function (button) {

                button.addEventListener("click", function () {

                    shownMonth.setMonth(
                        shownMonth.getMonth() +
                        Number(button.dataset.calendarShift)
                    );

                    renderCalendar();
                });

            });

        const todayButton =
            document.querySelector("[data-calendar-today]");

        todayButton.addEventListener("click", function () {

            shownMonth = new Date();
            shownMonth.setDate(1);

            renderCalendar();
        });

        document.querySelectorAll("[data-cal-type]")
            .forEach(function (checkbox) {

                checkbox.addEventListener(
                    "change",
                    function () {
                        renderCalendar();

                        if (selectedDate) {
                            showDayDetails(selectedDate);
                        }
                    }
                );

            });

        const viewButtons =
            document.querySelectorAll("[data-calendar-view-btn]");

        const panels =
            document.querySelectorAll("[data-calendar-panel]");

        viewButtons.forEach(function (button) {

            button.addEventListener("click", function () {

                const requestedView =
                    button.dataset.calendarViewBtn;

                viewButtons.forEach(function (other) {
                    other.classList.remove("active");
                });

                button.classList.add("active");

                panels.forEach(function (panel) {

                    panel.hidden =
                        panel.dataset.calendarPanel !== requestedView;

                });

            });

        });

        renderCalendar();
    }


    // ==========================================
    // 3. NOTIFICATIONS FILTERS
    // ==========================================

    const notificationRows =
        document.querySelectorAll("[data-notice-row]");

    if (notificationRows.length > 0) {

        const filterButtons =
            document.querySelectorAll("[data-notice-filter]");

        let selectedFilter = "all";

        function filterNotifications() {

            let visibleCount = 0;

            notificationRows.forEach(function (row) {

                const match =
                    selectedFilter === "all" ||
                    (selectedFilter === "unread" &&
                     row.dataset.read === "false") ||
                    row.dataset.type === selectedFilter;

                row.hidden = !match;

                if (match) {
                    visibleCount++;
                }

            });

            document.getElementById("noticeEmpty").hidden =
                visibleCount !== 0;
        }

        function updateUnreadCount() {

            const unread = Array.from(notificationRows)
                .filter(row => row.dataset.read === "false")
                .length;

            document.getElementById("unreadCount").textContent =
                unread;
        }

        filterButtons.forEach(function (button) {

            button.addEventListener("click", function () {

                selectedFilter = button.dataset.noticeFilter;

                filterButtons.forEach(function (other) {
                    other.classList.remove("active");
                });

                button.classList.add("active");

                filterNotifications();
            });

        });

        document.getElementById("markAllRead")
            .addEventListener("click", function () {

                notificationRows.forEach(function (row) {
                    row.dataset.read = "true";
                    row.classList.remove("unread");
                });

                updateUnreadCount();
                filterNotifications();

            });

        updateUnreadCount();
    }


    // ==========================================
    // 4. SETTINGS TABS
    // ==========================================

    const settingsTabs =
        document.querySelectorAll("[data-settings-tab]");

    if (settingsTabs.length > 0) {

        const settingsPanels =
            document.querySelectorAll("[data-settings-panel]");

        function openSettingsTab(name) {

            const exists = Array.from(settingsTabs).some(
                button => button.dataset.settingsTab === name
            );

            if (!exists) {
                name = "profile";
            }

            settingsTabs.forEach(function (button) {
                button.classList.toggle(
                    "active",
                    button.dataset.settingsTab === name
                );
            });

            settingsPanels.forEach(function (panel) {
                panel.hidden =
                    panel.dataset.settingsPanel !== name;
            });

            if (window.location.hash !== "#" + name) {
                history.replaceState(null, "", "#" + name);
            }
        }

        settingsTabs.forEach(function (tab) {

            tab.addEventListener("click", function () {
                openSettingsTab(tab.dataset.settingsTab);
            });

        });

        document.querySelectorAll("[data-settings-go]")
            .forEach(function (button) {

                button.addEventListener("click", function () {
                    openSettingsTab(button.dataset.settingsGo);
                });

            });

        document.querySelectorAll("[data-demo-save]")
            .forEach(function (button) {

                button.addEventListener("click", function () {

                    document.getElementById(
                        "settingsDemoMessage"
                    ).hidden = false;

                });

            });

        window.addEventListener("hashchange", function () {
            openSettingsTab(
                window.location.hash.replace("#", "")
            );
        });

        openSettingsTab(
            window.location.hash.replace("#", "") || "profile"
        );

    }


    // ==========================================
    // 5. HELP CENTER SEARCH
    // ==========================================

    const helpSearch =
        document.getElementById("helpSearch");

    if (helpSearch) {

        const articles =
            document.querySelectorAll("[data-help-article]");

        function filterHelp() {

            const term =
                helpSearch.value.trim().toLowerCase();

            let visibleCount = 0;

            articles.forEach(function (article) {

                const show =
                    article.textContent.toLowerCase().includes(term);

                article.hidden = !show;

                if (show) {
                    visibleCount++;
                }

            });

            document.getElementById("helpEmpty").hidden =
                visibleCount !== 0;
        }

        helpSearch.addEventListener("input", filterHelp);

        document.getElementById("helpSearchButton")
            .addEventListener("click", filterHelp);

    }

});
