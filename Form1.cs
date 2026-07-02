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
    private static readonly Color Peach = Color.FromArgb(247, 164, 145);
    private static readonly Color Coral = Color.FromArgb(224, 111, 94);
    private static readonly Color Cream = Color.FromArgb(255, 249, 244);
    private static readonly Color Blush = Color.FromArgb(255, 235, 225);
    private static readonly Color Ink = Color.FromArgb(75, 58, 55);
    private static readonly Color Muted = Color.FromArgb(137, 112, 106);
    private static readonly Color Line = Color.FromArgb(238, 216, 205);

    private readonly ComboBox collectionSelector = new();
    private readonly TextBox connectionStringBox = new();
    private readonly TextBox searchBox = new();
    private readonly DataGridView dataGrid = new();
    private readonly DataGridView overviewGrid = new();
    private readonly FlowLayoutPanel fieldsPanel = new();
    private readonly Label statusLabel = new();
    private readonly Dictionary<string, Control> fieldInputs = new();
    private readonly TextBox customerNameBox = new();
    private readonly TextBox customerPhoneBox = new();
    private readonly TextBox customerEmailBox = new();
    private readonly TextBox customerSeatBox = new();
    private readonly ComboBox customerOriginSelector = new();
    private readonly ComboBox customerDestinationSelector = new();
    private readonly ComboBox customerFlightSelector = new();
    private List<CustomerFlightOption> customerFlights = new();
    private bool updatingCustomerRoutes;

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
        collectionSelector.SelectedItem = "customers";
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
    private CollectionDefinition CurrentCollection => collections[(string)collectionSelector.SelectedItem!];

    private void BuildInterface()
    {
        Text = "Momo Air • Reservation Studio";
        Width = 1280;
        Height = 800;
        MinimumSize = new Size(1050, 650);
        Font = new Font("Segoe UI", 10F);
        BackColor = Cream;
        ForeColor = Ink;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Cream };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        Controls.Add(root);

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Peach, Padding = new Padding(26, 12, 26, 10) };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
        var brand = new Label
        {
            Text = "\uD83C\uDF51  momo air   \u2708",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var tagline = new Label
        {
            Text = "RESERVATION STUDIO",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleRight
        };
        header.Controls.Add(brand, 0, 0);
        header.Controls.Add(tagline, 1, 0);
        root.Controls.Add(header, 0, 0);

        var connectionPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, BackColor = Cream, Padding = new Padding(22, 12, 22, 8), Visible = false };
        connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115));
        connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        root.Controls.Add(connectionPanel, 0, 1);

        connectionPanel.Controls.Add(new Label { Text = "Database", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Muted, Font = new Font("Segoe UI Semibold", 10F) }, 0, 0);
        connectionStringBox.Dock = DockStyle.Fill;
        connectionStringBox.Text = "mongodb+srv://momoplane:momomongo@cluster0.bhly1yt.mongodb.net/?appName=Cluster0&serverSelectionTimeoutMS=10000&connectTimeoutMS=10000";
        StyleInput(connectionStringBox);
        connectionPanel.Controls.Add(connectionStringBox, 1, 0);

        var testButton = CreateButton("Test connection", false);
        testButton.Click += async (_, _) => await TestConnectionAsync();
        connectionPanel.Controls.Add(testButton, 2, 0);

        var initButton = CreateButton("Initialize data", true);
        initButton.Click += (_, _) => InitializeDatabase();
        connectionPanel.Controls.Add(initButton, 3, 0);

        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(18, 8), Font = new Font("Segoe UI Semibold", 10F), ItemSize = new Size(190, 36), SizeMode = TabSizeMode.Fixed };
        root.Controls.Add(tabs, 0, 2);

        var customerTab = new TabPage("Customer booking") { BackColor = Cream };
        var maintenanceTab = new TabPage("Staff management") { BackColor = Cream };
        var overviewTab = new TabPage("Staff overview") { BackColor = Cream };
        tabs.TabPages.Add(customerTab);
        tabs.TabPages.Add(maintenanceTab);
        tabs.TabPages.Add(overviewTab);
        tabs.SelectedTab = customerTab;

        BuildCustomerTab(customerTab);
        BuildMaintenanceTab(maintenanceTab);
        BuildOverviewTab(overviewTab);

        void UpdateConnectionVisibility()
        {
            var isCustomerView = tabs.SelectedTab == customerTab;
            connectionPanel.Visible = !isCustomerView;
            root.RowStyles[1].Height = isCustomerView ? 0 : 64;
        }

        tabs.SelectedIndexChanged += (_, _) => UpdateConnectionVisibility();
        UpdateConnectionVisibility();

        statusLabel.Dock = DockStyle.Fill;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusLabel.BackColor = Blush;
        statusLabel.ForeColor = Muted;
        statusLabel.Padding = new Padding(24, 0, 0, 0);
        statusLabel.Text = "Ready for takeoff.";
        root.Controls.Add(statusLabel, 0, 3);
    }

    private void BuildCustomerTab(TabPage tab)
    {
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(38, 26, 38, 26),
            BackColor = Cream
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        tab.Controls.Add(shell);

        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 11,
            Padding = new Padding(34, 26, 34, 26),
            BackColor = Color.White
        };
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        for (var i = 0; i < 7; i++)
        {
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        }
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.Controls.Add(card, 1, 0);

        card.Controls.Add(new Label
        {
            Text = "Where are we flying today?",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 20F),
            ForeColor = Ink,
            TextAlign = ContentAlignment.MiddleCenter
        }, 0, 0);
        card.Controls.Add(new Label
        {
            Text = "Enter your details and choose one of our available journeys.",
            Dock = DockStyle.Fill,
            ForeColor = Muted,
            TextAlign = ContentAlignment.TopCenter
        }, 0, 1);

        AddCustomerField(card, "Full name", customerNameBox, 2, "e.g. Hana Mori");
        AddCustomerField(card, "Phone", customerPhoneBox, 3, "e.g. 090-1234-5678");
        AddCustomerField(card, "Email", customerEmailBox, 4, "e.g. hana@example.com");

        customerOriginSelector.DropDownStyle = ComboBoxStyle.DropDownList;
        customerDestinationSelector.DropDownStyle = ComboBoxStyle.DropDownList;
        customerFlightSelector.DropDownStyle = ComboBoxStyle.DropDownList;
        customerOriginSelector.SelectedIndexChanged += (_, _) => FilterCustomerFlights();
        customerDestinationSelector.SelectedIndexChanged += (_, _) => FilterCustomerFlights();
        AddCustomerField(card, "From", customerOriginSelector, 5);
        AddCustomerField(card, "To", customerDestinationSelector, 6);
        AddCustomerField(card, "Choose flight", customerFlightSelector, 7);
        AddCustomerField(card, "Preferred seat", customerSeatBox, 8, "e.g. 12A");

        var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));
        var loadFlights = CreateButton("Load flights", false);
        loadFlights.Click += (_, _) => LoadCustomerFlights();
        var bookFlight = CreateButton("Book my flight  \u2708", true);
        bookFlight.Click += (_, _) => CreateCustomerBooking();
        actions.Controls.Add(loadFlights, 0, 0);
        actions.Controls.Add(bookFlight, 1, 0);
        card.Controls.Add(actions, 0, 9);

        card.Controls.Add(new Label
        {
            Text = "\uD83C\uDF51  Your booking will be saved securely to Momo Air.",
            Dock = DockStyle.Fill,
            ForeColor = Muted,
            Font = new Font("Segoe UI", 9F),
            TextAlign = ContentAlignment.MiddleCenter
        }, 0, 10);
    }

    private static void AddCustomerField(TableLayoutPanel panel, string label, Control input, int row, string? placeholder = null)
    {
        var group = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Margin = new Padding(0, 2, 0, 2) };
        group.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        group.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        group.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            ForeColor = Muted,
            Font = new Font("Segoe UI Semibold", 9F)
        }, 0, 0);
        input.Dock = DockStyle.Fill;
        StyleInput(input);
        if (input is TextBox box && placeholder is not null)
        {
            box.PlaceholderText = placeholder;
        }
        group.Controls.Add(input, 0, 1);
        panel.Controls.Add(group, 0, row);
    }

    private void BuildMaintenanceTab(TabPage tab)
    {
        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 400, BackColor = Line, Padding = new Padding(14, 12, 14, 14) };
        tab.Controls.Add(split);

        split.Panel1.BackColor = Color.White;
        split.Panel2.BackColor = Color.White;
        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, Padding = new Padding(18), BackColor = Color.White };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        split.Panel1.Controls.Add(left);

        left.Controls.Add(new Label { Text = "Record details", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 16F), ForeColor = Ink }, 0, 0);
        collectionSelector.DropDownStyle = ComboBoxStyle.DropDownList;
        collectionSelector.Items.AddRange(collections.Keys.Cast<object>().ToArray());
        collectionSelector.Dock = DockStyle.Fill;
        StyleInput(collectionSelector);
        collectionSelector.SelectedIndexChanged += (_, _) => RebuildFields();
        left.Controls.Add(collectionSelector, 0, 1);

        fieldsPanel.Dock = DockStyle.Fill;
        fieldsPanel.FlowDirection = FlowDirection.TopDown;
        fieldsPanel.WrapContents = false;
        fieldsPanel.AutoScroll = true;
        fieldsPanel.BackColor = Color.White;
        left.Controls.Add(fieldsPanel, 0, 2);

        var buttons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        left.Controls.Add(buttons, 0, 3);

        AddActionButton(buttons, "Add", 0, 0, AddRecord);
        AddActionButton(buttons, "Update", 1, 0, UpdateRecord);
        AddActionButton(buttons, "Delete", 0, 1, DeleteRecord);
        AddActionButton(buttons, "Clear", 1, 1, ClearFields);

        var refreshButton = CreateButton("Refresh collection", false);
        refreshButton.Click += (_, _) => LoadCurrentCollection();
        left.Controls.Add(refreshButton, 0, 4);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(18), BackColor = Color.White };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        split.Panel2.Controls.Add(right);

        right.Controls.Add(new Label { Text = "Collection", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 16F), ForeColor = Ink }, 0, 0);
        searchBox.Dock = DockStyle.Fill;
        searchBox.PlaceholderText = "Search this collection…";
        StyleInput(searchBox);
        searchBox.TextChanged += (_, _) => LoadCurrentCollection();
        right.Controls.Add(searchBox, 0, 1);

        StyleGrid(dataGrid);
        dataGrid.SelectionChanged += (_, _) => FillFieldsFromSelectedRow();
        right.Controls.Add(dataGrid, 0, 2);
    }

    private void BuildOverviewTab(TabPage tab)
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(24, 18, 24, 24), BackColor = Cream };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tab.Controls.Add(root);

        root.Controls.Add(new Label { Text = "All journeys, one lovely view", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 18F), ForeColor = Ink }, 0, 0);
        var refresh = CreateButton("Refresh overview", true);
        refresh.Dock = DockStyle.Left;
        refresh.Width = 190;
        refresh.Click += (_, _) => LoadOverview();
        root.Controls.Add(refresh, 0, 1);

        StyleGrid(overviewGrid);
        root.Controls.Add(overviewGrid, 0, 2);
    }

    private void AddActionButton(TableLayoutPanel panel, string text, int column, int row, Action action)
    {
        var button = CreateButton(text, text is "Add" or "Update");
        button.Click += (_, _) => action();
        panel.Controls.Add(button, column, row);
    }

    private Button CreateButton(string text, bool primary)
    {
        return new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Margin = new Padding(5),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Coral : Blush,
            ForeColor = primary ? Color.White : Ink,
            Font = new Font("Segoe UI Semibold", 9.5F),
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = primary ? 0 : 1, BorderColor = Line }
        };
    }

    private static void StyleInput(Control control)
    {
        control.Font = new Font("Segoe UI", 10F);
        control.BackColor = Color.White;
        control.ForeColor = Ink;
        control.Margin = new Padding(4, 5, 4, 7);
        control.Padding = new Padding(6);
    }

    private static void StyleGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.RowHeadersVisible = false;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = Line;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Blush;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Ink;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(4);
        grid.ColumnHeadersHeight = 42;
        grid.DefaultCellStyle.BackColor = Color.White;
        grid.DefaultCellStyle.ForeColor = Ink;
        grid.DefaultCellStyle.SelectionBackColor = Peach;
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.DefaultCellStyle.Padding = new Padding(4);
        grid.RowTemplate.Height = 38;
    }

    private void RebuildFields()
    {
        fieldsPanel.Controls.Clear();
        fieldInputs.Clear();

        foreach (var field in CurrentCollection.Fields)
        {
            fieldsPanel.Controls.Add(new Label { Text = field.Label, Width = 330, Height = 24, ForeColor = Muted, Font = new Font("Segoe UI Semibold", 9F) });
            Control input = field.Kind switch
            {
                FieldKind.Choice => new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 330 },
                FieldKind.Lookup => new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 330 },
                FieldKind.DateTime => new DateTimePicker { Width = 330, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm:ss", ShowUpDown = true },
                _ => new TextBox { Width = 330 }
            };
            StyleInput(input);

            if (input is ComboBox combo && field.Choices.Length > 0)
            {
                combo.Items.AddRange(field.Choices.Cast<object>().ToArray());
                combo.SelectedIndex = 0;
            }

            fieldsPanel.Controls.Add(input);
            fieldInputs[field.Name] = input;
        }

        LoadLookups();
        ClearFields();
        LoadCurrentCollection();
    }

    private IMongoCollection<BsonDocument> GetCollection(string name) => Database.GetCollection<BsonDocument>(name);

    private void LoadCustomerFlights()
    {
        try
        {
            customerFlights = GetCollection("flights")
                .Find(FilterDefinition<BsonDocument>.Empty)
                .Sort(Builders<BsonDocument>.Sort.Ascending("departure_time"))
                .ToList()
                .Select(flight => new CustomerFlightOption(
                    flight.GetValue("flight_id", 0).ToInt32(),
                    ReadString(flight, "origin"),
                    ReadString(flight, "destination"),
                    $"{ReadString(flight, "flight_number")}  •  {ReadString(flight, "origin")} → {ReadString(flight, "destination")}  •  {ReadDateTime(flight, "departure_time"):MMM d, HH:mm}"))
                .ToList();

            updatingCustomerRoutes = true;
            try
            {
                var origins = customerFlights.Select(flight => flight.Origin).Distinct().OrderBy(city => city).Cast<object>().ToArray();
                customerOriginSelector.Items.Clear();
                customerOriginSelector.Items.AddRange(origins);
                if (customerOriginSelector.Items.Count > 0)
                {
                    customerOriginSelector.SelectedIndex = 0;
                }
            }
            finally
            {
                updatingCustomerRoutes = false;
            }

            FilterCustomerFlights();
            SetStatus(customerFlights.Count == 0 ? "No flights are available yet. Ask staff to initialize the database." : $"Loaded {customerFlights.Count} domestic Japan flights.");
        }
        catch (Exception ex)
        {
            SetStatus($"Could not load flights: {ex.GetBaseException().Message}");
        }
    }

    private void RefreshCustomerDestinations()
    {
        var origin = customerOriginSelector.SelectedItem?.ToString();
        var destinations = customerFlights
            .Where(flight => origin is null || flight.Origin == origin)
            .Select(flight => flight.Destination)
            .Distinct()
            .OrderBy(city => city)
            .Cast<object>()
            .ToArray();

        var previous = customerDestinationSelector.SelectedItem?.ToString();
        customerDestinationSelector.Items.Clear();
        customerDestinationSelector.Items.AddRange(destinations);
        if (previous is not null && customerDestinationSelector.Items.Contains(previous))
        {
            customerDestinationSelector.SelectedItem = previous;
        }
        else if (customerDestinationSelector.Items.Count > 0)
        {
            customerDestinationSelector.SelectedIndex = 0;
        }
    }

    private void FilterCustomerFlights()
    {
        if (updatingCustomerRoutes)
        {
            return;
        }

        if (customerFlights.Count == 0)
        {
            customerFlightSelector.DataSource = null;
            return;
        }

        updatingCustomerRoutes = true;
        try
        {
            RefreshCustomerDestinations();
            var origin = customerOriginSelector.SelectedItem?.ToString();
            var destination = customerDestinationSelector.SelectedItem?.ToString();
            var matchingFlights = customerFlights
                .Where(flight => flight.Origin == origin && flight.Destination == destination)
                .Select(flight => new LookupRow(flight.Id, flight.Label))
                .ToList();

            customerFlightSelector.DisplayMember = "Label";
            customerFlightSelector.ValueMember = "Id";
            customerFlightSelector.DataSource = matchingFlights;
        }
        finally
        {
            updatingCustomerRoutes = false;
        }
    }

    private void CreateCustomerBooking()
    {
        var fullName = customerNameBox.Text.Trim();
        var phone = customerPhoneBox.Text.Trim();
        var email = customerEmailBox.Text.Trim();
        var seat = customerSeatBox.Text.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(phone) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(seat) ||
            customerFlightSelector.SelectedValue is not int flightId)
        {
            SetStatus("Please complete every booking field and choose a flight.");
            MessageBox.Show("Please complete every booking field and choose a flight.", "Booking details needed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!email.Contains('@') || !email.Contains('.'))
        {
            SetStatus("Please enter a valid email address.");
            return;
        }

        try
        {
            var customerDefinition = collections["customers"];
            var reservationDefinition = collections["reservations"];
            var ticketDefinition = collections["tickets"];
            var customerId = NextId(customerDefinition);
            var reservationId = NextId(reservationDefinition);
            var ticketId = NextId(ticketDefinition);

            GetCollection("customers").InsertOne(new BsonDocument
            {
                ["customer_id"] = customerId,
                ["full_name"] = fullName,
                ["phone"] = phone,
                ["email"] = email
            });
            GetCollection("reservations").InsertOne(new BsonDocument
            {
                ["reservation_id"] = reservationId,
                ["reservation_code"] = $"RSV-{1000 + reservationId}",
                ["customer_id"] = customerId,
                ["flight_id"] = flightId,
                ["reservation_status"] = "Reserved",
                ["reservation_date"] = DateTime.Now
            });
            GetCollection("tickets").InsertOne(new BsonDocument
            {
                ["ticket_id"] = ticketId,
                ["ticket_number"] = $"TKT-{9000 + ticketId}",
                ["reservation_id"] = reservationId,
                ["seat_number"] = seat,
                ["fare_amount"] = BsonDecimal128.Create(15000m),
                ["ticket_status"] = "Issued",
                ["boarding_status"] = "NotBoarded"
            });

            customerNameBox.Clear();
            customerPhoneBox.Clear();
            customerEmailBox.Clear();
            customerSeatBox.Clear();
            LoadCurrentCollection();
            LoadOverview();
            SetStatus($"Booking confirmed: RSV-{1000 + reservationId}, seat {seat}.");
            MessageBox.Show(
                $"Your Momo Air booking is confirmed!\n\nReservation: RSV-{1000 + reservationId}\nTicket: TKT-{9000 + ticketId}\nSeat: {seat}",
                "Ready for takeoff",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            SetStatus($"Booking failed: {ex.GetBaseException().Message}");
        }
    }

    private void LoadLookups()
    {
        foreach (var field in CurrentCollection.Fields.Where(f => f.Kind == FieldKind.Lookup))
        {
            if (fieldInputs[field.Name] is not ComboBox combo || field.LookupCollection is null)
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

    private void LoadCurrentCollection()
    {
        if (collectionSelector.SelectedItem is null)
        {
            return;
        }

        try
        {
            var definition = CurrentCollection;
            var documents = GetCollection(definition.Name)
                .Find(BuildSearchFilter(definition, searchBox.Text.Trim()))
                .Sort(Builders<BsonDocument>.Sort.Descending(definition.PrimaryKey))
                .ToList();

            dataGrid.DataSource = ToDataTable(documents, definition.PrimaryKey, definition.Fields.Select(f => f.Name));
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
            SetStatus("Loaded reservation lookup view.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private void AddRecord()
    {
        try
        {
            var definition = CurrentCollection;
            var document = BuildDocument(definition);
            document[definition.PrimaryKey] = NextId(definition);
            GetCollection(definition.Name).InsertOne(document);
            LoadLookups();
            LoadCurrentCollection();
            LoadOverview();
            SetStatus("Record added.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private void UpdateRecord()
    {
        if (SelectedId() is not int id)
        {
            SetStatus("Select a row first.");
            return;
        }

        try
        {
            var definition = CurrentCollection;
            var updateParts = definition.Fields.Select(field => Builders<BsonDocument>.Update.Set(field.Name, ReadInputValue(field))).ToList();
            var update = Builders<BsonDocument>.Update.Combine(updateParts);
            GetCollection(definition.Name).UpdateOne(Builders<BsonDocument>.Filter.Eq(definition.PrimaryKey, id), update);
            LoadLookups();
            LoadCurrentCollection();
            LoadOverview();
            SetStatus("Record updated.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private void DeleteRecord()
    {
        if (SelectedId() is not int id)
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
            var definition = CurrentCollection;
            GetCollection(definition.Name).DeleteOne(Builders<BsonDocument>.Filter.Eq(definition.PrimaryKey, id));
            LoadLookups();
            LoadCurrentCollection();
            LoadOverview();
            SetStatus("Record deleted.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private BsonDocument BuildDocument(CollectionDefinition definition)
    {
        var document = new BsonDocument();
        foreach (var field in definition.Fields)
        {
            document[field.Name] = ReadInputValue(field);
        }

        return document;
    }

    private BsonValue ReadInputValue(Field field)
    {
        var control = fieldInputs[field.Name];
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

    private void FillFieldsFromSelectedRow()
    {
        if (dataGrid.CurrentRow?.DataBoundItem is not DataRowView row)
        {
            return;
        }

        foreach (var field in CurrentCollection.Fields)
        {
            if (!row.Row.Table.Columns.Contains(field.Name) || row[field.Name] == DBNull.Value)
            {
                continue;
            }

            var value = row[field.Name];
            var control = fieldInputs[field.Name];

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

    private int? SelectedId()
    {
        if (dataGrid.CurrentRow?.DataBoundItem is not DataRowView row)
        {
            return null;
        }

        var idValue = row[CurrentCollection.PrimaryKey];
        return idValue == DBNull.Value ? null : Convert.ToInt32(idValue);
    }

    private void ClearFields()
    {
        foreach (var field in CurrentCollection.Fields)
        {
            var control = fieldInputs[field.Name];
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

    private async Task TestConnectionAsync()
    {
        SetStatus("Connecting to MongoDB Atlas…");
        try
        {
            MongoDbService.ResetInstance();
            var service = MongoDbService.GetInstance(ConnectionString, DatabaseName);
            await service.Database.Client
                .GetDatabase("admin")
                .RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
            SetStatus("✓ MongoDB connected successfully.");
            LoadCustomerFlights();
        }
        catch (Exception ex)
        {
            var reason = ex.ToString().Contains("Local Security Authority cannot be contacted", StringComparison.OrdinalIgnoreCase)
                ? "Windows could not start a secure TLS connection. Restart Windows and make sure Cryptographic Services is running."
                : ex.GetBaseException().Message;
            SetStatus($"Connection failed: {reason}");
            MessageBox.Show(
                $"{reason}\n\nCheck your Atlas database user/password and add your current IP address under Network Access.",
                "MongoDB connection failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
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
                new BsonDocument { ["flight_id"] = 3, ["flight_number"] = "BB330", ["origin"] = "Tokyo", ["destination"] = "Sapporo", ["departure_time"] = new DateTime(2026, 7, 3, 18, 15, 0), ["arrival_time"] = new DateTime(2026, 7, 3, 19, 50, 0), ["seat_capacity"] = 200 },
                new BsonDocument { ["flight_id"] = 4, ["flight_number"] = "BB118", ["origin"] = "Tokyo", ["destination"] = "Fukuoka", ["departure_time"] = new DateTime(2026, 7, 4, 8, 20, 0), ["arrival_time"] = new DateTime(2026, 7, 4, 10, 15, 0), ["seat_capacity"] = 180 },
                new BsonDocument { ["flight_id"] = 5, ["flight_number"] = "BB142", ["origin"] = "Tokyo", ["destination"] = "Okinawa", ["departure_time"] = new DateTime(2026, 7, 4, 11, 10, 0), ["arrival_time"] = new DateTime(2026, 7, 4, 14, 5, 0), ["seat_capacity"] = 200 },
                new BsonDocument { ["flight_id"] = 6, ["flight_number"] = "BB221", ["origin"] = "Osaka", ["destination"] = "Okinawa", ["departure_time"] = new DateTime(2026, 7, 5, 10, 0, 0), ["arrival_time"] = new DateTime(2026, 7, 5, 12, 15, 0), ["seat_capacity"] = 180 },
                new BsonDocument { ["flight_id"] = 7, ["flight_number"] = "BB304", ["origin"] = "Sapporo", ["destination"] = "Tokyo", ["departure_time"] = new DateTime(2026, 7, 5, 15, 30, 0), ["arrival_time"] = new DateTime(2026, 7, 5, 17, 10, 0), ["seat_capacity"] = 200 },
                new BsonDocument { ["flight_id"] = 8, ["flight_number"] = "BB410", ["origin"] = "Fukuoka", ["destination"] = "Tokyo", ["departure_time"] = new DateTime(2026, 7, 6, 9, 40, 0), ["arrival_time"] = new DateTime(2026, 7, 6, 11, 25, 0), ["seat_capacity"] = 180 },
                new BsonDocument { ["flight_id"] = 9, ["flight_number"] = "BB515", ["origin"] = "Nagoya", ["destination"] = "Sapporo", ["departure_time"] = new DateTime(2026, 7, 6, 12, 20, 0), ["arrival_time"] = new DateTime(2026, 7, 6, 14, 5, 0), ["seat_capacity"] = 160 },
                new BsonDocument { ["flight_id"] = 10, ["flight_number"] = "BB608", ["origin"] = "Hiroshima", ["destination"] = "Tokyo", ["departure_time"] = new DateTime(2026, 7, 7, 16, 0, 0), ["arrival_time"] = new DateTime(2026, 7, 7, 17, 25, 0), ["seat_capacity"] = 160 }
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

            LoadLookups();
            LoadCustomerFlights();
            LoadCurrentCollection();
            LoadOverview();
            SetStatus("MongoDB database initialized with fictional sample data.");
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

    private sealed record LookupRow(int Id, string Label);
    private sealed record CustomerFlightOption(int Id, string Origin, string Destination, string Label);
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
}


