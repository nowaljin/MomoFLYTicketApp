using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AirlineTicketReservationSystem;

public partial class Form1 : Form
{
    private const string DatabaseName = "airline_reservation_db";

    // ---- Theme -------------------------------------------------------
    private static readonly Color SidebarColor = Color.FromArgb(13, 21, 54);
    private static readonly Color SidebarActiveColor = Color.FromArgb(33, 47, 99);
    private static readonly Color SidebarTextColor = Color.FromArgb(180, 188, 214);
    private static readonly Color AccentOrange = Color.FromArgb(226, 87, 42);
    private static readonly Color AccentBlue = Color.FromArgb(70, 110, 200);
    private static readonly Color PageBackColor = Color.FromArgb(245, 247, 250);
    private static readonly Color StripBackColor = Color.FromArgb(238, 240, 245);
    private static readonly Color TextDark = Color.FromArgb(24, 30, 48);
    private static readonly Color TextMuted = Color.FromArgb(110, 118, 140);

    // ---- Chrome controls ----------------------------------------------
    private readonly TextBox connectionStringBox = new();
    private readonly Label statusLabel = new();
    private readonly Panel contentHost = new();
    private readonly FlowLayoutPanel breadcrumbFlow = new();
    private readonly Label pageTitleLabel = new();
    private readonly Label pageDescLabel = new();

    private readonly Dictionary<string, Panel> pages = new();
    private readonly Dictionary<string, Button> navButtons = new();
    private readonly Dictionary<string, CrudContext> crudContexts = new();
    private string currentPageKey = "main";

    // ---- Boarding controls ----------------------------------------------
    private readonly TextBox boardingSearchBox = new();
    private readonly DataGridView boardingGrid = new();

    // ---- Cancellation controls -------------------------------------------
    private readonly DataGridView cancellationReservationGrid = new();
    private readonly DataGridView cancellationTicketGrid = new();

    // ---- History controls -------------------------------------------------
    private readonly TextBox historySearchBox = new();
    private readonly DataGridView overviewGrid = new();

    private readonly List<NavItem> navItems = new()
    {
        new("main", "\u2302", "Main Menu", "Select a function to begin.", false),
        new("customers", "\U0001F465", "Customer Management", "Register, search & edit passengers", true),
        new("flights", "\u2708", "Flight Management", "Manage routes, schedules & fares", true),
        new("bookings", "\U0001F4C5", "Booking Registration", "Create seat reservations", true),
        new("tickets", "\U0001F3AB", "Ticket Issuance", "Issue tickets for confirmed bookings", true),
        new("boarding", "\u2705", "Boarding", "Search tickets & process boarding", true),
        new("cancellation", "\u2716", "Cancellation", "Cancel reservations & tickets", true),
        new("history", "\U0001F551", "History", "Customer booking & travel history", true),
    };

    private readonly Dictionary<string, CollectionDefinition> collections = new()
    {
        ["customers"] = new CollectionDefinition(
            "customers",
            "customer_id",
            doc => $"{doc.GetValue("customer_id", 0)} - {doc.GetValue("full_name", "")}",
            new[]
            {
                Field.Text("full_name", "Full Name"),
                Field.Text("phone", "Phone"),
                Field.Text("email", "Email")
            },
            new[] { "full_name", "phone", "email" }),

        ["flights"] = new CollectionDefinition(
            "flights",
            "flight_id",
            doc => $"{doc.GetValue("flight_id", 0)} - {doc.GetValue("flight_number", "")} {doc.GetValue("origin", "")} to {doc.GetValue("destination", "")}",
            new[]
            {
                Field.Text("flight_number", "Flight Number"),
                Field.Text("origin", "Origin"),
                Field.Text("destination", "Destination"),
                Field.DateTime("departure_time", "Departure Time"),
                Field.DateTime("arrival_time", "Arrival Time"),
                Field.Number("seat_capacity", "Seat Capacity")
            },
            new[] { "flight_number", "origin", "destination" }),

        ["reservations"] = new CollectionDefinition(
            "reservations",
            "reservation_id",
            doc => $"{doc.GetValue("reservation_id", 0)} - {doc.GetValue("reservation_code", "")}",
            new[]
            {
                Field.Text("reservation_code", "Reservation Code"),
                Field.Lookup("customer_id", "Customer", "customers"),
                Field.Lookup("flight_id", "Flight", "flights"),
                Field.Choice("reservation_status", "Reservation Status", "Reserved", "Cancelled", "CheckedIn"),
                Field.DateTime("reservation_date", "Reservation Date")
            },
            new[] { "reservation_code", "reservation_status" }),

        ["tickets"] = new CollectionDefinition(
            "tickets",
            "ticket_id",
            doc => $"{doc.GetValue("ticket_id", 0)} - {doc.GetValue("ticket_number", "")}",
            new[]
            {
                Field.Text("ticket_number", "Ticket Number"),
                Field.Lookup("reservation_id", "Reservation", "reservations"),
                Field.Text("seat_number", "Seat Number"),
                Field.Decimal("fare_amount", "Fare Amount"),
                Field.Choice("ticket_status", "Ticket Status", "Issued", "Cancelled", "Refunded"),
                Field.Choice("boarding_status", "Boarding Status", "NotBoarded", "Boarded")
            },
            new[] { "ticket_number", "seat_number", "ticket_status", "boarding_status" })
    };

    public Form1()
    {
        InitializeComponent();
        BuildInterface();
        ShowPage("main");
    }

    private string ConnectionString => connectionStringBox.Text.Trim();
    private IMongoDatabase Database
    {
        get
        {
            var service = MongoDbService.GetInstance(ConnectionString, DatabaseName);
            return service.Database;
        }
    }

    // =====================================================================
    // Layout
    // =====================================================================

    private void BuildInterface()
    {
        Text = "Momo Fly - Ops System";
        Width = 1360;
        Height = 900;
        MinimumSize = new Size(1100, 700);
        Font = new Font("Segoe UI", 9.5F);
        BackColor = PageBackColor;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(BuildSidebar(), 0, 0);
        root.Controls.Add(BuildMainArea(), 1, 0);
    }

    private Panel BuildSidebar()
    {
        var sidebar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = SidebarColor };
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));

        // Logo
        var logo = new Panel { Dock = DockStyle.Fill, BackColor = SidebarColor, Padding = new Padding(18, 14, 10, 10) };
        var logoIcon = new Label
        {
            Text = "\u2708",
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = AccentOrange,
            Size = new Size(36, 36),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(18, 16)
        };
        var titleLabel = new Label
        {
            Text = "MOMO FLY",
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(62, 15)
        };
        var subLabel = new Label
        {
            Text = "OPS SYSTEM",
            Font = new Font("Segoe UI", 7.5F),
            ForeColor = SidebarTextColor,
            AutoSize = true,
            Location = new Point(62, 34)
        };
        logo.Controls.Add(logoIcon);
        logo.Controls.Add(titleLabel);
        logo.Controls.Add(subLabel);

        // Nav list
        var navFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = SidebarColor,
            Padding = new Padding(8, 8, 8, 8)
        };

        foreach (var item in navItems)
        {
            var button = new Button
            {
                Text = "   " + item.Icon + "   " + item.Title,
                Width = 200,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = SidebarColor,
                ForeColor = SidebarTextColor,
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(0, 2, 0, 2),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            var key = item.Key;
            button.Click += (_, _) => ShowPage(key);
            navButtons[key] = button;
            navFlow.Controls.Add(button);
        }

        // Footer
        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = SidebarColor, Padding = new Padding(18, 10, 10, 10) };
        footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        var adminLabel = new Label { Text = "ADMIN USER", Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(90, 98, 128), AutoSize = true };
        var exitButton = new Button
        {
            Text = "\u2192]  Exit",
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = SidebarColor,
            ForeColor = SidebarTextColor,
            Font = new Font("Segoe UI", 9.5F),
            Width = 180,
            Height = 26,
            Cursor = Cursors.Hand
        };
        exitButton.FlatAppearance.BorderSize = 0;
        exitButton.Click += (_, _) => Close();
        footer.Controls.Add(adminLabel, 0, 0);
        footer.Controls.Add(exitButton, 0, 1);

        sidebar.Controls.Add(logo, 0, 0);
        sidebar.Controls.Add(navFlow, 0, 1);
        sidebar.Controls.Add(footer, 0, 2);
        return sidebar;
    }

    private Panel BuildMainArea()
    {
        var mainArea = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = PageBackColor };
        mainArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        mainArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        mainArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        mainArea.Controls.Add(BuildConnectionStrip(), 0, 0);
        mainArea.Controls.Add(BuildHeader(), 0, 1);

        contentHost.Dock = DockStyle.Fill;
        contentHost.BackColor = PageBackColor;
        mainArea.Controls.Add(contentHost, 0, 2);

        statusLabel.Dock = DockStyle.Fill;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusLabel.Padding = new Padding(12, 0, 0, 0);
        statusLabel.ForeColor = TextMuted;
        statusLabel.BackColor = StripBackColor;
        mainArea.Controls.Add(statusLabel, 0, 3);

        BuildPages();

        return mainArea;
    }

    private Panel BuildConnectionStrip()
    {
        var strip = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, BackColor = StripBackColor, Padding = new Padding(10, 5, 10, 5) };
        strip.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        strip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        strip.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        strip.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

        strip.Controls.Add(new Label { Text = "MongoDB URI", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = TextMuted, Font = new Font("Segoe UI", 8.5F) }, 0, 0);

        connectionStringBox.Dock = DockStyle.Fill;
        connectionStringBox.Font = new Font("Segoe UI", 8.5F);
        connectionStringBox.Text = "mongodb+srv://momoplane:momomongo@cluster0.bhly1yt.mongodb.net/?appName=Cluster0&serverSelectionTimeoutMS=10000&connectTimeoutMS=10000";
        strip.Controls.Add(connectionStringBox, 1, 0);

        var testButton = new Button { Text = "Test", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat };
        testButton.Click += (_, _) => TestConnection();
        strip.Controls.Add(testButton, 2, 0);

        var initButton = new Button { Text = "Initialize DB", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat };
        initButton.Click += (_, _) => InitializeDatabase();
        strip.Controls.Add(initButton, 3, 0);

        return strip;
    }

    private Panel BuildHeader()
    {
        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

        breadcrumbFlow.FlowDirection = FlowDirection.LeftToRight;
        breadcrumbFlow.AutoSize = true;
        breadcrumbFlow.WrapContents = false;
        breadcrumbFlow.Location = new Point(24, 10);
        breadcrumbFlow.BackColor = Color.White;
        header.Controls.Add(breadcrumbFlow);

        pageTitleLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        pageTitleLabel.ForeColor = TextDark;
        pageTitleLabel.AutoSize = true;
        pageTitleLabel.Location = new Point(24, 30);
        header.Controls.Add(pageTitleLabel);

        pageDescLabel.Font = new Font("Segoe UI", 9F);
        pageDescLabel.ForeColor = TextMuted;
        pageDescLabel.AutoSize = true;
        pageDescLabel.Location = new Point(24, 58);
        header.Controls.Add(pageDescLabel);

        var separator = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(228, 231, 237) };
        header.Controls.Add(separator);

        return header;
    }

    private void BuildPages()
    {
        pages["main"] = CreateMainMenuPage();
        pages["customers"] = CreateCrudPage("customers", collections["customers"]);
        pages["flights"] = CreateCrudPage("flights", collections["flights"]);
        pages["bookings"] = CreateCrudPage("bookings", collections["reservations"]);
        pages["tickets"] = CreateCrudPage("tickets", collections["tickets"]);
        pages["boarding"] = CreateBoardingPage();
        pages["cancellation"] = CreateCancellationPage();
        pages["history"] = CreateHistoryPage();

        foreach (var page in pages.Values)
        {
            page.Dock = DockStyle.Fill;
            page.Visible = false;
            contentHost.Controls.Add(page);
        }
    }

    // =====================================================================
    // Navigation
    // =====================================================================

    private void ShowPage(string key)
    {
        if (!pages.ContainsKey(key))
        {
            return;
        }

        currentPageKey = key;

        foreach (var kvp in pages)
        {
            kvp.Value.Visible = kvp.Key == key;
        }
        pages[key].BringToFront();

        foreach (var kvp in navButtons)
        {
            var active = kvp.Key == key;
            kvp.Value.BackColor = active ? SidebarActiveColor : SidebarColor;
            kvp.Value.ForeColor = active ? Color.White : SidebarTextColor;
            kvp.Value.Font = new Font("Segoe UI", 9.5F, active ? FontStyle.Bold : FontStyle.Regular);
        }

        var item = navItems.First(n => n.Key == key);

        breadcrumbFlow.Controls.Clear();
        breadcrumbFlow.Controls.Add(new Label { Text = "Momo Fly", AutoSize = true, ForeColor = AccentBlue, Font = new Font("Segoe UI", 8.5F) });
        breadcrumbFlow.Controls.Add(new Label { Text = " / ", AutoSize = true, ForeColor = TextMuted, Font = new Font("Segoe UI", 8.5F) });
        breadcrumbFlow.Controls.Add(new Label { Text = item.Title, AutoSize = true, ForeColor = TextDark, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) });

        pageTitleLabel.Text = item.Title;
        pageDescLabel.Text = item.Subtitle;

        RefreshPageData(key);
    }

    private void RefreshPageData(string key)
    {
        try
        {
            switch (key)
            {
                case "customers":
                case "flights":
                case "bookings":
                case "tickets":
                    var ctx = crudContexts[key];
                    LoadLookupsFor(ctx);
                    LoadCollection(ctx);
                    break;
                case "boarding":
                    LoadBoardingGrid();
                    break;
                case "cancellation":
                    LoadCancellationGrids();
                    break;
                case "history":
                    LoadOverview();
                    break;
            }
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    // =====================================================================
    // Main menu
    // =====================================================================

    private Panel CreateMainMenuPage()
    {
        var page = new Panel { AutoScroll = true, BackColor = PageBackColor };

        var layout = new TableLayoutPanel
        {
            Location = new Point(0, 0),
            AutoSize = true,
            ColumnCount = 3,
            Padding = new Padding(24, 20, 24, 20)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        foreach (var item in navItems.Where(n => n.ShowOnMainMenu))
        {
            layout.Controls.Add(BuildCard(item));
        }

        page.Controls.Add(layout);
        return page;
    }

    private Panel BuildCard(NavItem item)
    {
        var card = new Panel
        {
            Width = 260,
            Height = 150,
            Margin = new Padding(0, 0, 16, 16),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Cursor = Cursors.Hand
        };

        var iconBox = new Panel
        {
            Size = new Size(44, 44),
            Location = new Point(18, 18),
            BackColor = Color.FromArgb(240, 242, 247)
        };
        var iconLabel = new Label
        {
            Text = item.Icon,
            Font = new Font("Segoe UI", 15F),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = TextDark
        };
        iconBox.Controls.Add(iconLabel);

        var titleLabel = new Label
        {
            Text = item.Title,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = TextDark,
            Location = new Point(18, 74),
            Size = new Size(224, 22)
        };

        var subtitleLabel = new Label
        {
            Text = item.Subtitle,
            Font = new Font("Segoe UI", 8.75F),
            ForeColor = TextMuted,
            Location = new Point(18, 96),
            Size = new Size(224, 34)
        };

        var arrowLabel = new Label
        {
            Text = "\u2192",
            Font = new Font("Segoe UI", 11F),
            ForeColor = TextMuted,
            Location = new Point(18, 122),
            AutoSize = true
        };

        card.Controls.Add(iconBox);
        card.Controls.Add(titleLabel);
        card.Controls.Add(subtitleLabel);
        card.Controls.Add(arrowLabel);

        var key = item.Key;
        void Navigate(object? s, EventArgs e) => ShowPage(key);
        card.Click += Navigate;
        iconBox.Click += Navigate;
        iconLabel.Click += Navigate;
        titleLabel.Click += Navigate;
        subtitleLabel.Click += Navigate;
        arrowLabel.Click += Navigate;

        return card;
    }

    // =====================================================================
    // Generic CRUD pages (Customers / Flights / Bookings / Ticket Issuance)
    // =====================================================================

    private Panel CreateCrudPage(string navKey, CollectionDefinition definition)
    {
        var ctx = new CrudContext(definition);
        crudContexts[navKey] = ctx;

        var page = new Panel { BackColor = PageBackColor };
        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 360, BackColor = PageBackColor };
        page.Controls.Add(split);

        // Left: form card
        var leftOuter = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16), BackColor = PageBackColor };
        split.Panel1.Controls.Add(leftOuter);
        var leftCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(14) };
        leftOuter.Controls.Add(leftCard);

        var leftLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        leftCard.Controls.Add(leftLayout);

        ctx.FieldsPanel.Dock = DockStyle.Fill;
        ctx.FieldsPanel.FlowDirection = FlowDirection.TopDown;
        ctx.FieldsPanel.WrapContents = false;
        ctx.FieldsPanel.AutoScroll = true;
        leftLayout.Controls.Add(ctx.FieldsPanel, 0, 0);

        var buttons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        leftLayout.Controls.Add(buttons, 0, 1);

        AddActionButton(buttons, "Add", 0, 0, () => AddRecord(ctx), true);
        AddActionButton(buttons, "Update", 1, 0, () => UpdateRecord(ctx), false);
        AddActionButton(buttons, "Delete", 0, 1, () => DeleteRecord(ctx), false);
        AddActionButton(buttons, "Clear", 1, 1, () => ClearFields(ctx), false);

        var refreshButton = new Button { Text = "Refresh List", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat };
        refreshButton.Click += (_, _) => LoadCollection(ctx);
        leftLayout.Controls.Add(refreshButton, 0, 2);

        // Right: search + grid card
        var rightOuter = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16), BackColor = PageBackColor };
        split.Panel2.Controls.Add(rightOuter);
        var rightCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(14) };
        rightOuter.Controls.Add(rightCard);

        var rightLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rightCard.Controls.Add(rightLayout);

        ctx.SearchBox.Dock = DockStyle.Fill;
        ctx.SearchBox.PlaceholderText = "Search...";
        ctx.SearchBox.TextChanged += (_, _) => LoadCollection(ctx);
        rightLayout.Controls.Add(ctx.SearchBox, 0, 0);

        ctx.Grid.Dock = DockStyle.Fill;
        ctx.Grid.ReadOnly = true;
        ctx.Grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        ctx.Grid.MultiSelect = false;
        ctx.Grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        ctx.Grid.SelectionChanged += (_, _) => FillFieldsFromSelectedRow(ctx);
        rightLayout.Controls.Add(ctx.Grid, 0, 1);

        BuildFieldControls(ctx);

        return page;
    }

    private static void AddActionButton(TableLayoutPanel panel, string text, int column, int row, Action action, bool primary)
    {
        var button = new Button { Text = text, Dock = DockStyle.Fill, Margin = new Padding(4), FlatStyle = FlatStyle.Flat };
        if (primary)
        {
            button.BackColor = AccentOrange;
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderSize = 0;
        }
        button.Click += (_, _) => action();
        panel.Controls.Add(button, column, row);
    }

    private void BuildFieldControls(CrudContext ctx)
    {
        ctx.FieldsPanel.Controls.Clear();
        ctx.FieldInputs.Clear();

        foreach (var field in ctx.Definition.Fields)
        {
            ctx.FieldsPanel.Controls.Add(new Label
            {
                Text = field.Label,
                Width = 300,
                Height = 20,
                Font = new Font("Segoe UI", 8.75F, FontStyle.Bold),
                ForeColor = TextMuted,
                Margin = new Padding(0, 8, 0, 2)
            });

            Control input = field.Kind switch
            {
                FieldKind.Choice => new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 },
                FieldKind.Lookup => new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 },
                FieldKind.DateTime => new DateTimePicker { Width = 300, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm:ss", ShowUpDown = true },
                _ => new TextBox { Width = 300 }
            };

            if (input is ComboBox combo && field.Choices.Length > 0)
            {
                combo.Items.AddRange(field.Choices.Cast<object>().ToArray());
                combo.SelectedIndex = 0;
            }

            ctx.FieldsPanel.Controls.Add(input);
            ctx.FieldInputs[field.Name] = input;
        }
    }

    private IMongoCollection<BsonDocument> GetCollection(string name) => Database.GetCollection<BsonDocument>(name);

    private void LoadLookupsFor(CrudContext ctx)
    {
        foreach (var field in ctx.Definition.Fields.Where(f => f.Kind == FieldKind.Lookup))
        {
            if (ctx.FieldInputs[field.Name] is not ComboBox combo || field.LookupCollection is null)
            {
                continue;
            }

            combo.DisplayMember = "Label";
            combo.ValueMember = "Id";
            combo.DataSource = GetLookupRows(collections[field.LookupCollection]);
        }
    }

    private List<LookupRow> GetLookupRows(CollectionDefinition definition)
    {
        try
        {
            return GetCollection(definition.Name)
                .Find(FilterDefinition<BsonDocument>.Empty)
                .Sort(Builders<BsonDocument>.Sort.Ascending(definition.PrimaryKey))
                .ToList()
                .Select(doc => new LookupRow(doc.GetValue(definition.PrimaryKey, 0).ToInt32(), definition.LookupLabel(doc)))
                .ToList();
        }
        catch
        {
            return new List<LookupRow> { new(0, "Run Initialize DB first") };
        }
    }

    private void LoadCollection(CrudContext ctx)
    {
        try
        {
            var definition = ctx.Definition;
            var documents = GetCollection(definition.Name)
                .Find(BuildSearchFilter(definition, ctx.SearchBox.Text.Trim()))
                .Sort(Builders<BsonDocument>.Sort.Descending(definition.PrimaryKey))
                .ToList();

            ctx.Grid.DataSource = ToDataTable(documents, definition.PrimaryKey, definition.Fields.Select(f => f.Name));
            SetStatus($"Loaded {definition.Name}.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private static FilterDefinition<BsonDocument> BuildSearchFilter(CollectionDefinition definition, string search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return FilterDefinition<BsonDocument>.Empty;
        }

        var regex = new BsonRegularExpression(search, "i");
        return Builders<BsonDocument>.Filter.Or(definition.SearchFields.Select(field => Builders<BsonDocument>.Filter.Regex(field, regex)));
    }

    private void AddRecord(CrudContext ctx)
    {
        try
        {
            var definition = ctx.Definition;
            var document = BuildDocument(ctx);
            document[definition.PrimaryKey] = NextId(definition);
            GetCollection(definition.Name).InsertOne(document);
            LoadLookupsFor(ctx);
            LoadCollection(ctx);
            SetStatus("Record added.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private void UpdateRecord(CrudContext ctx)
    {
        if (SelectedId(ctx) is not int id)
        {
            SetStatus("Select a row first.");
            return;
        }

        try
        {
            var definition = ctx.Definition;
            var updateParts = definition.Fields.Select(field => Builders<BsonDocument>.Update.Set(field.Name, ReadInputValue(ctx, field))).ToList();
            var update = Builders<BsonDocument>.Update.Combine(updateParts);
            GetCollection(definition.Name).UpdateOne(Builders<BsonDocument>.Filter.Eq(definition.PrimaryKey, id), update);
            LoadLookupsFor(ctx);
            LoadCollection(ctx);
            SetStatus("Record updated.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private void DeleteRecord(CrudContext ctx)
    {
        if (SelectedId(ctx) is not int id)
        {
            SetStatus("Select a row first.");
            return;
        }

        if (MessageBox.Show("Delete the selected record?", "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            var definition = ctx.Definition;
            GetCollection(definition.Name).DeleteOne(Builders<BsonDocument>.Filter.Eq(definition.PrimaryKey, id));
            LoadLookupsFor(ctx);
            LoadCollection(ctx);
            SetStatus("Record deleted.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private BsonDocument BuildDocument(CrudContext ctx)
    {
        var document = new BsonDocument();
        foreach (var field in ctx.Definition.Fields)
        {
            document[field.Name] = ReadInputValue(ctx, field);
        }

        return document;
    }

    private BsonValue ReadInputValue(CrudContext ctx, Field field)
    {
        var control = ctx.FieldInputs[field.Name];
        return field.Kind switch
        {
            FieldKind.Lookup when control is ComboBox combo && combo.SelectedValue is int lookupId => lookupId,
            FieldKind.Choice when control is ComboBox combo => combo.SelectedItem?.ToString() ?? "",
            FieldKind.DateTime when control is DateTimePicker picker => picker.Value,
            FieldKind.Number when control is TextBox box && int.TryParse(box.Text, out var number) => number,
            FieldKind.Decimal when control is TextBox box && decimal.TryParse(box.Text, out var amount) => BsonDecimal128.Create(amount),
            _ when control is TextBox box => box.Text.Trim(),
            _ => BsonNull.Value
        };
    }

    private int NextId(CollectionDefinition definition)
    {
        var latest = GetCollection(definition.Name)
            .Find(FilterDefinition<BsonDocument>.Empty)
            .Sort(Builders<BsonDocument>.Sort.Descending(definition.PrimaryKey))
            .Limit(1)
            .FirstOrDefault();

        return latest is null ? 1 : latest.GetValue(definition.PrimaryKey, 0).ToInt32() + 1;
    }

    private int? SelectedId(CrudContext ctx)
    {
        if (ctx.Grid.CurrentRow?.DataBoundItem is not DataRowView row)
        {
            return null;
        }

        var idValue = row[ctx.Definition.PrimaryKey];
        return idValue == DBNull.Value ? null : Convert.ToInt32(idValue);
    }

    private void ClearFields(CrudContext ctx)
    {
        foreach (var field in ctx.Definition.Fields)
        {
            var control = ctx.FieldInputs[field.Name];
            switch (control)
            {
                case TextBox box:
                    box.Clear();
                    break;
                case ComboBox combo when combo.Items.Count > 0:
                    combo.SelectedIndex = 0;
                    break;
                case DateTimePicker picker:
                    picker.Value = DateTime.Now;
                    break;
            }
        }
    }

    private void FillFieldsFromSelectedRow(CrudContext ctx)
    {
        if (ctx.Grid.CurrentRow?.DataBoundItem is not DataRowView row)
        {
            return;
        }

        foreach (var field in ctx.Definition.Fields)
        {
            if (!row.Row.Table.Columns.Contains(field.Name) || row[field.Name] == DBNull.Value)
            {
                continue;
            }

            var value = row[field.Name];
            var control = ctx.FieldInputs[field.Name];

            if (control is DateTimePicker picker && DateTime.TryParse(value.ToString(), out var dateTime))
            {
                picker.Value = dateTime;
            }
            else if (control is ComboBox combo)
            {
                if (field.Kind == FieldKind.Lookup && int.TryParse(value.ToString(), out var lookupId))
                {
                    combo.SelectedValue = lookupId;
                }
                else
                {
                    combo.SelectedItem = value.ToString();
                }
            }
            else if (control is TextBox box)
            {
                box.Text = value.ToString();
            }
        }
    }

    // =====================================================================
    // Boarding page
    // =====================================================================

    private Panel CreateBoardingPage()
    {
        var page = new Panel { BackColor = PageBackColor, Padding = new Padding(16) };
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(14) };
        page.Controls.Add(card);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        card.Controls.Add(layout);

        var searchRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        boardingSearchBox.Dock = DockStyle.Fill;
        boardingSearchBox.PlaceholderText = "Search by ticket number or seat...";
        boardingSearchBox.TextChanged += (_, _) => LoadBoardingGrid();
        searchRow.Controls.Add(boardingSearchBox, 0, 0);
        var refreshButton = new Button { Text = "Refresh", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Margin = new Padding(6, 0, 0, 0) };
        refreshButton.Click += (_, _) => LoadBoardingGrid();
        searchRow.Controls.Add(refreshButton, 1, 0);
        layout.Controls.Add(searchRow, 0, 0);

        boardingGrid.Dock = DockStyle.Fill;
        boardingGrid.ReadOnly = true;
        boardingGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        boardingGrid.MultiSelect = false;
        boardingGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        layout.Controls.Add(boardingGrid, 0, 1);

        var actionRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        var boardButton = new Button { Text = "Mark as Boarded", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = AccentOrange, ForeColor = Color.White, Margin = new Padding(0, 6, 4, 0) };
        boardButton.FlatAppearance.BorderSize = 0;
        boardButton.Click += (_, _) => SetBoardingStatus("Boarded");
        var unboardButton = new Button { Text = "Undo Boarding", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Margin = new Padding(4, 6, 0, 0) };
        unboardButton.Click += (_, _) => SetBoardingStatus("NotBoarded");
        actionRow.Controls.Add(boardButton, 0, 0);
        actionRow.Controls.Add(unboardButton, 1, 0);
        layout.Controls.Add(actionRow, 0, 2);

        return page;
    }

    private DataTable BuildBoardingTable(string search)
    {
        var reservations = GetCollection("reservations").Find(FilterDefinition<BsonDocument>.Empty).ToList().ToDictionary(d => d["reservation_id"].ToInt32());
        var customers = GetCollection("customers").Find(FilterDefinition<BsonDocument>.Empty).ToList().ToDictionary(d => d["customer_id"].ToInt32());
        var flights = GetCollection("flights").Find(FilterDefinition<BsonDocument>.Empty).ToList().ToDictionary(d => d["flight_id"].ToInt32());
        var tickets = GetCollection("tickets").Find(FilterDefinition<BsonDocument>.Empty).Sort(Builders<BsonDocument>.Sort.Descending("ticket_id")).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            tickets = tickets.Where(t => ReadString(t, "ticket_number").Contains(term, StringComparison.OrdinalIgnoreCase)
                                       || ReadString(t, "seat_number").Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var table = new DataTable();
        foreach (var column in new[] { "ticket_id", "ticket_number", "seat_number", "ticket_status", "boarding_status", "reservation_code", "customer_name", "flight_number" })
        {
            table.Columns.Add(column);
        }

        foreach (var ticket in tickets)
        {
            reservations.TryGetValue(ticket.GetValue("reservation_id", 0).ToInt32(), out var reservation);
            BsonDocument? customer = null;
            BsonDocument? flight = null;
            if (reservation is not null)
            {
                customers.TryGetValue(reservation.GetValue("customer_id", 0).ToInt32(), out customer);
                flights.TryGetValue(reservation.GetValue("flight_id", 0).ToInt32(), out flight);
            }

            var row = table.NewRow();
            row["ticket_id"] = ticket.GetValue("ticket_id", 0).ToInt32();
            row["ticket_number"] = ReadString(ticket, "ticket_number");
            row["seat_number"] = ReadString(ticket, "seat_number");
            row["ticket_status"] = ReadString(ticket, "ticket_status");
            row["boarding_status"] = ReadString(ticket, "boarding_status");
            row["reservation_code"] = reservation is null ? "" : ReadString(reservation, "reservation_code");
            row["customer_name"] = customer is null ? "" : ReadString(customer, "full_name");
            row["flight_number"] = flight is null ? "" : ReadString(flight, "flight_number");
            table.Rows.Add(row);
        }

        return table;
    }

    private void LoadBoardingGrid()
    {
        try
        {
            boardingGrid.DataSource = BuildBoardingTable(boardingSearchBox.Text.Trim());
            SetStatus("Loaded boarding list.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private void SetBoardingStatus(string status)
    {
        if (boardingGrid.CurrentRow?.DataBoundItem is not DataRowView row)
        {
            SetStatus("Select a ticket first.");
            return;
        }

        try
        {
            var ticketId = Convert.ToInt32(row["ticket_id"]);
            GetCollection("tickets").UpdateOne(
                Builders<BsonDocument>.Filter.Eq("ticket_id", ticketId),
                Builders<BsonDocument>.Update.Set("boarding_status", status));
            LoadBoardingGrid();
            SetStatus(status == "Boarded" ? "Passenger marked as boarded." : "Boarding status reset.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    // =====================================================================
    // Cancellation page
    // =====================================================================

    private Panel CreateCancellationPage()
    {
        var page = new Panel { BackColor = PageBackColor, Padding = new Padding(16) };
        var tabs = new TabControl { Dock = DockStyle.Fill };
        page.Controls.Add(tabs);

        var reservationsTab = new TabPage("Reservations");
        var ticketsTab = new TabPage("Tickets");
        tabs.TabPages.Add(reservationsTab);
        tabs.TabPages.Add(ticketsTab);

        var resLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(10) };
        resLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        resLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        reservationsTab.Controls.Add(resLayout);

        cancellationReservationGrid.Dock = DockStyle.Fill;
        cancellationReservationGrid.ReadOnly = true;
        cancellationReservationGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        cancellationReservationGrid.MultiSelect = false;
        cancellationReservationGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        resLayout.Controls.Add(cancellationReservationGrid, 0, 0);

        var cancelReservationButton = new Button { Text = "Cancel Selected Reservation", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(200, 60, 60), ForeColor = Color.White, Margin = new Padding(0, 6, 0, 0) };
        cancelReservationButton.FlatAppearance.BorderSize = 0;
        cancelReservationButton.Click += (_, _) => CancelReservation();
        resLayout.Controls.Add(cancelReservationButton, 0, 1);

        var tickLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(10) };
        tickLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tickLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        ticketsTab.Controls.Add(tickLayout);

        cancellationTicketGrid.Dock = DockStyle.Fill;
        cancellationTicketGrid.ReadOnly = true;
        cancellationTicketGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        cancellationTicketGrid.MultiSelect = false;
        cancellationTicketGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        tickLayout.Controls.Add(cancellationTicketGrid, 0, 0);

        var cancelTicketButton = new Button { Text = "Cancel Selected Ticket", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(200, 60, 60), ForeColor = Color.White, Margin = new Padding(0, 6, 0, 0) };
        cancelTicketButton.FlatAppearance.BorderSize = 0;
        cancelTicketButton.Click += (_, _) => CancelTicket();
        tickLayout.Controls.Add(cancelTicketButton, 0, 1);

        return page;
    }

    private void LoadCancellationGrids()
    {
        try
        {
            var customers = GetCollection("customers").Find(FilterDefinition<BsonDocument>.Empty).ToList().ToDictionary(d => d["customer_id"].ToInt32());
            var flights = GetCollection("flights").Find(FilterDefinition<BsonDocument>.Empty).ToList().ToDictionary(d => d["flight_id"].ToInt32());
            var reservations = GetCollection("reservations").Find(FilterDefinition<BsonDocument>.Empty).Sort(Builders<BsonDocument>.Sort.Descending("reservation_id")).ToList();

            var resTable = new DataTable();
            foreach (var column in new[] { "reservation_id", "reservation_code", "customer_name", "flight_number", "reservation_status", "reservation_date" })
            {
                resTable.Columns.Add(column);
            }

            foreach (var reservation in reservations)
            {
                customers.TryGetValue(reservation.GetValue("customer_id", 0).ToInt32(), out var customer);
                flights.TryGetValue(reservation.GetValue("flight_id", 0).ToInt32(), out var flight);

                var row = resTable.NewRow();
                row["reservation_id"] = reservation.GetValue("reservation_id", 0).ToInt32();
                row["reservation_code"] = ReadString(reservation, "reservation_code");
                row["customer_name"] = customer is null ? "" : ReadString(customer, "full_name");
                row["flight_number"] = flight is null ? "" : ReadString(flight, "flight_number");
                row["reservation_status"] = ReadString(reservation, "reservation_status");
                row["reservation_date"] = FormatValue(reservation.GetValue("reservation_date", BsonNull.Value));
                resTable.Rows.Add(row);
            }

            cancellationReservationGrid.DataSource = resTable;

            var reservationsById = reservations.ToDictionary(r => r["reservation_id"].ToInt32());
            var tickets = GetCollection("tickets").Find(FilterDefinition<BsonDocument>.Empty).Sort(Builders<BsonDocument>.Sort.Descending("ticket_id")).ToList();

            var tickTable = new DataTable();
            foreach (var column in new[] { "ticket_id", "ticket_number", "seat_number", "reservation_code", "ticket_status", "boarding_status" })
            {
                tickTable.Columns.Add(column);
            }

            foreach (var ticket in tickets)
            {
                reservationsById.TryGetValue(ticket.GetValue("reservation_id", 0).ToInt32(), out var reservation);

                var row = tickTable.NewRow();
                row["ticket_id"] = ticket.GetValue("ticket_id", 0).ToInt32();
                row["ticket_number"] = ReadString(ticket, "ticket_number");
                row["seat_number"] = ReadString(ticket, "seat_number");
                row["reservation_code"] = reservation is null ? "" : ReadString(reservation, "reservation_code");
                row["ticket_status"] = ReadString(ticket, "ticket_status");
                row["boarding_status"] = ReadString(ticket, "boarding_status");
                tickTable.Rows.Add(row);
            }

            cancellationTicketGrid.DataSource = tickTable;
            SetStatus("Loaded cancellation lists.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private void CancelReservation()
    {
        if (cancellationReservationGrid.CurrentRow?.DataBoundItem is not DataRowView row)
        {
            SetStatus("Select a reservation first.");
            return;
        }

        if (MessageBox.Show("Cancel the selected reservation?", "Confirm cancellation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            var id = Convert.ToInt32(row["reservation_id"]);
            GetCollection("reservations").UpdateOne(
                Builders<BsonDocument>.Filter.Eq("reservation_id", id),
                Builders<BsonDocument>.Update.Set("reservation_status", "Cancelled"));
            LoadCancellationGrids();
            SetStatus("Reservation cancelled.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private void CancelTicket()
    {
        if (cancellationTicketGrid.CurrentRow?.DataBoundItem is not DataRowView row)
        {
            SetStatus("Select a ticket first.");
            return;
        }

        if (MessageBox.Show("Cancel the selected ticket?", "Confirm cancellation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            var id = Convert.ToInt32(row["ticket_id"]);
            GetCollection("tickets").UpdateOne(
                Builders<BsonDocument>.Filter.Eq("ticket_id", id),
                Builders<BsonDocument>.Update.Set("ticket_status", "Cancelled"));
            LoadCancellationGrids();
            SetStatus("Ticket cancelled.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    // =====================================================================
    // History page
    // =====================================================================

    private Panel CreateHistoryPage()
    {
        var page = new Panel { BackColor = PageBackColor, Padding = new Padding(16) };
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(14) };
        page.Controls.Add(card);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.Controls.Add(layout);

        var searchRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        historySearchBox.Dock = DockStyle.Fill;
        historySearchBox.PlaceholderText = "Filter by customer, flight, reservation or ticket...";
        historySearchBox.TextChanged += (_, _) => ApplyHistoryFilter();
        searchRow.Controls.Add(historySearchBox, 0, 0);
        var refreshButton = new Button { Text = "Refresh", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Margin = new Padding(6, 0, 0, 0) };
        refreshButton.Click += (_, _) => LoadOverview();
        searchRow.Controls.Add(refreshButton, 1, 0);
        layout.Controls.Add(searchRow, 0, 0);

        overviewGrid.Dock = DockStyle.Fill;
        overviewGrid.ReadOnly = true;
        overviewGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        overviewGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        layout.Controls.Add(overviewGrid, 0, 1);

        return page;
    }

    private void LoadOverview()
    {
        try
        {
            var customers = GetCollection("customers").Find(FilterDefinition<BsonDocument>.Empty).ToList().ToDictionary(d => d["customer_id"].ToInt32());
            var flights = GetCollection("flights").Find(FilterDefinition<BsonDocument>.Empty).ToList().ToDictionary(d => d["flight_id"].ToInt32());
            var tickets = GetCollection("tickets").Find(FilterDefinition<BsonDocument>.Empty).ToList().GroupBy(d => d["reservation_id"].ToInt32()).ToDictionary(g => g.Key, g => g.First());
            var reservations = GetCollection("reservations").Find(FilterDefinition<BsonDocument>.Empty).Sort(Builders<BsonDocument>.Sort.Ascending("reservation_code")).ToList();

            var table = new DataTable();
            foreach (var column in new[] { "ReservationCode", "CustomerName", "FlightNumber", "Origin", "Destination", "DepartureTime", "TicketNumber", "SeatNumber", "ReservationStatus", "TicketStatus", "BoardingStatus" })
            {
                table.Columns.Add(column);
            }

            foreach (var reservation in reservations)
            {
                customers.TryGetValue(reservation["customer_id"].ToInt32(), out var customer);
                flights.TryGetValue(reservation["flight_id"].ToInt32(), out var flight);
                tickets.TryGetValue(reservation["reservation_id"].ToInt32(), out var ticket);

                table.Rows.Add(
                    ReadString(reservation, "reservation_code"),
                    customer is null ? "" : ReadString(customer, "full_name"),
                    flight is null ? "" : ReadString(flight, "flight_number"),
                    flight is null ? "" : ReadString(flight, "origin"),
                    flight is null ? "" : ReadString(flight, "destination"),
                    flight is null ? "" : ReadDateTime(flight, "departure_time").ToString("yyyy-MM-dd HH:mm:ss"),
                    ticket is null ? "" : ReadString(ticket, "ticket_number"),
                    ticket is null ? "" : ReadString(ticket, "seat_number"),
                    ReadString(reservation, "reservation_status"),
                    ticket is null ? "" : ReadString(ticket, "ticket_status"),
                    ticket is null ? "" : ReadString(ticket, "boarding_status"));
            }

            overviewGrid.DataSource = table;
            ApplyHistoryFilter();
            SetStatus("Loaded reservation history.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private void ApplyHistoryFilter()
    {
        if (overviewGrid.DataSource is not DataTable table)
        {
            return;
        }

        var term = historySearchBox.Text.Trim().Replace("'", "''");
        table.DefaultView.RowFilter = string.IsNullOrEmpty(term)
            ? ""
            : $"ReservationCode LIKE '%{term}%' OR CustomerName LIKE '%{term}%' OR FlightNumber LIKE '%{term}%' OR TicketNumber LIKE '%{term}%'";
    }

    // =====================================================================
    // Connection / seed data
    // =====================================================================

    private void TestConnection()
    {
        try
        {
            // Reset to reinitialize with any new connection string
            MongoDbService.ResetInstance();
            var service = MongoDbService.GetInstance(ConnectionString, DatabaseName);

            if (service.TestConnection())
            {
                SetStatus("\u2713 MongoDB connection successful. Server is responding.");
            }
            else
            {
                SetStatus("\u2717 MongoDB connection failed. Please check your connection string.");
            }
        }
        catch (Exception ex)
        {
            SetStatus($"\u2717 Connection error: {ex.Message}");
        }
    }

    private void InitializeDatabase()
    {
        try
        {
            var database = Database;
            foreach (var name in collections.Keys)
            {
                database.DropCollection(name);
            }

            database.GetCollection<BsonDocument>("customers").InsertMany(new[]
            {
                new BsonDocument { ["customer_id"] = 1, ["full_name"] = "Aylin Demir", ["phone"] = "090-1111-2222", ["email"] = "aylin.demir@example.com" },
                new BsonDocument { ["customer_id"] = 2, ["full_name"] = "Kenji Sato", ["phone"] = "090-3333-4444", ["email"] = "kenji.sato@example.com" },
                new BsonDocument { ["customer_id"] = 3, ["full_name"] = "Mina Carter", ["phone"] = "090-5555-6666", ["email"] = "mina.carter@example.com" }
            });

            database.GetCollection<BsonDocument>("flights").InsertMany(new[]
            {
                new BsonDocument { ["flight_id"] = 1, ["flight_number"] = "BB101", ["origin"] = "Tokyo", ["destination"] = "Osaka", ["departure_time"] = new DateTime(2026, 7, 1, 9, 0, 0), ["arrival_time"] = new DateTime(2026, 7, 1, 10, 20, 0), ["seat_capacity"] = 180 },
                new BsonDocument { ["flight_id"] = 2, ["flight_number"] = "BB205", ["origin"] = "Osaka", ["destination"] = "Fukuoka", ["departure_time"] = new DateTime(2026, 7, 2, 13, 30, 0), ["arrival_time"] = new DateTime(2026, 7, 2, 14, 45, 0), ["seat_capacity"] = 160 },
                new BsonDocument { ["flight_id"] = 3, ["flight_number"] = "BB330", ["origin"] = "Tokyo", ["destination"] = "Sapporo", ["departure_time"] = new DateTime(2026, 7, 3, 18, 15, 0), ["arrival_time"] = new DateTime(2026, 7, 3, 19, 50, 0), ["seat_capacity"] = 200 }
            });

            database.GetCollection<BsonDocument>("reservations").InsertMany(new[]
            {
                new BsonDocument { ["reservation_id"] = 1, ["reservation_code"] = "RSV-1001", ["customer_id"] = 1, ["flight_id"] = 1, ["reservation_status"] = "Reserved", ["reservation_date"] = new DateTime(2026, 6, 20, 10, 10, 0) },
                new BsonDocument { ["reservation_id"] = 2, ["reservation_code"] = "RSV-1002", ["customer_id"] = 2, ["flight_id"] = 2, ["reservation_status"] = "CheckedIn", ["reservation_date"] = new DateTime(2026, 6, 21, 11, 25, 0) },
                new BsonDocument { ["reservation_id"] = 3, ["reservation_code"] = "RSV-1003", ["customer_id"] = 3, ["flight_id"] = 3, ["reservation_status"] = "Cancelled", ["reservation_date"] = new DateTime(2026, 6, 22, 15, 40, 0) }
            });

            database.GetCollection<BsonDocument>("tickets").InsertMany(new[]
            {
                new BsonDocument { ["ticket_id"] = 1, ["ticket_number"] = "TKT-9001", ["reservation_id"] = 1, ["seat_number"] = "12A", ["fare_amount"] = BsonDecimal128.Create(14500m), ["ticket_status"] = "Issued", ["boarding_status"] = "NotBoarded" },
                new BsonDocument { ["ticket_id"] = 2, ["ticket_number"] = "TKT-9002", ["reservation_id"] = 2, ["seat_number"] = "08C", ["fare_amount"] = BsonDecimal128.Create(12800m), ["ticket_status"] = "Issued", ["boarding_status"] = "Boarded" },
                new BsonDocument { ["ticket_id"] = 3, ["ticket_number"] = "TKT-9003", ["reservation_id"] = 3, ["seat_number"] = "21F", ["fare_amount"] = BsonDecimal128.Create(17000m), ["ticket_status"] = "Cancelled", ["boarding_status"] = "NotBoarded" }
            });

            SetStatus("MongoDB database initialized with fictional sample data.");
            RefreshPageData(currentPageKey);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private static DataTable ToDataTable(IEnumerable<BsonDocument> documents, string primaryKey, IEnumerable<string> fields)
    {
        var table = new DataTable();
        table.Columns.Add(primaryKey, typeof(int));
        foreach (var field in fields)
        {
            table.Columns.Add(field);
        }

        foreach (var document in documents)
        {
            var row = table.NewRow();
            row[primaryKey] = document.GetValue(primaryKey, 0).ToInt32();
            foreach (var field in fields)
            {
                row[field] = FormatValue(document.GetValue(field, BsonNull.Value));
            }

            table.Rows.Add(row);
        }

        return table;
    }

    private static string FormatValue(BsonValue value)
    {
        if (value == BsonNull.Value)
        {
            return "";
        }

        if (value.IsValidDateTime)
        {
            return value.ToUniversalTime().ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        }

        return value.ToString();
    }

    private static string ReadString(BsonDocument document, string name) => document.GetValue(name, "").ToString();
    private static DateTime ReadDateTime(BsonDocument document, string name) => document.GetValue(name).ToUniversalTime().ToLocalTime();
    private void SetStatus(string message) => statusLabel.Text = message;

    private sealed record NavItem(string Key, string Icon, string Title, string Subtitle, bool ShowOnMainMenu);
    private sealed record LookupRow(int Id, string Label);
    private sealed record CollectionDefinition(string Name, string PrimaryKey, Func<BsonDocument, string> LookupLabel, Field[] Fields, string[] SearchFields);
    private sealed record Field(string Name, string Label, FieldKind Kind, string? LookupCollection = null, string[]? ChoiceValues = null)
    {
        public string[] Choices => ChoiceValues ?? Array.Empty<string>();

        public static Field Text(string name, string label) => new(name, label, FieldKind.Text);
        public static Field Number(string name, string label) => new(name, label, FieldKind.Number);
        public static Field Decimal(string name, string label) => new(name, label, FieldKind.Decimal);
        public static Field DateTime(string name, string label) => new(name, label, FieldKind.DateTime);
        public static Field Lookup(string name, string label, string lookupCollection) => new(name, label, FieldKind.Lookup, lookupCollection);
        public static Field Choice(string name, string label, params string[] choices) => new(name, label, FieldKind.Choice, ChoiceValues: choices);
    }

    private enum FieldKind
    {
        Text,
        Number,
        Decimal,
        DateTime,
        Lookup,
        Choice
    }

    private sealed class CrudContext
    {
        public CrudContext(CollectionDefinition definition)
        {
            Definition = definition;
        }

        public CollectionDefinition Definition { get; }
        public FlowLayoutPanel FieldsPanel { get; } = new();
        public DataGridView Grid { get; } = new();
        public TextBox SearchBox { get; } = new();
        public Dictionary<string, Control> FieldInputs { get; } = new();
    }
}
