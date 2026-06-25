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

    private readonly ComboBox collectionSelector = new();
    private readonly TextBox connectionStringBox = new();
    private readonly TextBox searchBox = new();
    private readonly DataGridView dataGrid = new();
    private readonly DataGridView overviewGrid = new();
    private readonly FlowLayoutPanel fieldsPanel = new();
    private readonly Label statusLabel = new();
    private readonly Dictionary<string, Control> fieldInputs = new();

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
        Text = "Airline Ticket Reservation Management System - MongoDB";
        Width = 1220;
        Height = 760;
        MinimumSize = new Size(1050, 650);
        Font = new Font("Segoe UI", 10F);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        Controls.Add(root);

        var connectionPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4 };
        connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        root.Controls.Add(connectionPanel, 0, 0);

        connectionPanel.Controls.Add(new Label { Text = "MongoDB URI", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        connectionStringBox.Dock = DockStyle.Fill;
        connectionStringBox.Text = "mongodb+srv://momoplane:momomongo@cluster0.bhly1yt.mongodb.net/?appName=Cluster0&serverSelectionTimeoutMS=10000&connectTimeoutMS=10000";
        connectionPanel.Controls.Add(connectionStringBox, 1, 0);

        var testButton = new Button { Text = "Test", Dock = DockStyle.Fill };
        testButton.Click += (_, _) => TestConnection();
        connectionPanel.Controls.Add(testButton, 2, 0);

        var initButton = new Button { Text = "Initialize DB", Dock = DockStyle.Fill };
        initButton.Click += (_, _) => InitializeDatabase();
        connectionPanel.Controls.Add(initButton, 3, 0);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        root.Controls.Add(tabs, 0, 1);

        var maintenanceTab = new TabPage("CRUD Management");
        var overviewTab = new TabPage("Reservation Lookup View");
        tabs.TabPages.Add(maintenanceTab);
        tabs.TabPages.Add(overviewTab);

        BuildMaintenanceTab(maintenanceTab);
        BuildOverviewTab(overviewTab);

        statusLabel.Dock = DockStyle.Fill;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(statusLabel, 0, 2);
    }

    private void BuildMaintenanceTab(TabPage tab)
    {
        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 390 };
        tab.Controls.Add(split);

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, Padding = new Padding(8) };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        split.Panel1.Controls.Add(left);

        collectionSelector.DropDownStyle = ComboBoxStyle.DropDownList;
        collectionSelector.Items.AddRange(collections.Keys.Cast<object>().ToArray());
        collectionSelector.Dock = DockStyle.Fill;
        collectionSelector.SelectedIndexChanged += (_, _) => RebuildFields();
        left.Controls.Add(collectionSelector, 0, 0);

        fieldsPanel.Dock = DockStyle.Fill;
        fieldsPanel.FlowDirection = FlowDirection.TopDown;
        fieldsPanel.WrapContents = false;
        fieldsPanel.AutoScroll = true;
        left.Controls.Add(fieldsPanel, 0, 1);

        var buttons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        left.Controls.Add(buttons, 0, 2);

        AddActionButton(buttons, "Add", 0, 0, AddRecord);
        AddActionButton(buttons, "Update", 1, 0, UpdateRecord);
        AddActionButton(buttons, "Delete", 0, 1, DeleteRecord);
        AddActionButton(buttons, "Clear", 1, 1, ClearFields);

        var refreshButton = new Button { Text = "Refresh Collection", Dock = DockStyle.Fill };
        refreshButton.Click += (_, _) => LoadCurrentCollection();
        left.Controls.Add(refreshButton, 0, 3);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(8) };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        split.Panel2.Controls.Add(right);

        searchBox.Dock = DockStyle.Fill;
        searchBox.TextChanged += (_, _) => LoadCurrentCollection();
        right.Controls.Add(searchBox, 0, 0);

        dataGrid.Dock = DockStyle.Fill;
        dataGrid.ReadOnly = true;
        dataGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dataGrid.MultiSelect = false;
        dataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dataGrid.SelectionChanged += (_, _) => FillFieldsFromSelectedRow();
        right.Controls.Add(dataGrid, 0, 1);
    }

    private void BuildOverviewTab(TabPage tab)
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(8) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tab.Controls.Add(root);

        var refresh = new Button { Text = "Refresh Lookup View", Dock = DockStyle.Left, Width = 200 };
        refresh.Click += (_, _) => LoadOverview();
        root.Controls.Add(refresh, 0, 0);

        overviewGrid.Dock = DockStyle.Fill;
        overviewGrid.ReadOnly = true;
        overviewGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        overviewGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        root.Controls.Add(overviewGrid, 0, 1);
    }

    private static void AddActionButton(TableLayoutPanel panel, string text, int column, int row, Action action)
    {
        var button = new Button { Text = text, Dock = DockStyle.Fill, Margin = new Padding(4) };
        button.Click += (_, _) => action();
        panel.Controls.Add(button, column, row);
    }

    private void RebuildFields()
    {
        fieldsPanel.Controls.Clear();
        fieldInputs.Clear();

        foreach (var field in CurrentCollection.Fields)
        {
            fieldsPanel.Controls.Add(new Label { Text = field.Label, Width = 330, Height = 24 });
            Control input = field.Kind switch
            {
                FieldKind.Choice => new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 330 },
                FieldKind.Lookup => new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 330 },
                FieldKind.DateTime => new DateTimePicker { Width = 330, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm:ss", ShowUpDown = true },
                _ => new TextBox { Width = 330 }
            };

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

    private void TestConnection()
    {
        try
        {
            // Reset to reinitialize with any new connection string
            MongoDbService.ResetInstance();
            var service = MongoDbService.GetInstance(ConnectionString, DatabaseName);
            
            if (service.TestConnection())
            {
                SetStatus("✓ MongoDB connection successful. Server is responding.");
            }
            else
            {
                SetStatus("✗ MongoDB connection failed. Please check your connection string.");
            }
        }
        catch (Exception ex)
        {
            SetStatus($"✗ Connection error: {ex.Message}");
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

            LoadLookups();
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


