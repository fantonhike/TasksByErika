using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OfflineCalendar
{
    public partial class TaskWindow : Window
    {
        public TaskItem Task { get; set; }
        private string selectedColor = "Red";  // default is Red

        public TaskWindow(TaskItem task = null)
        {
            InitializeComponent();
            if (task != null)
            {
                Task = task;
                TitleTextBox.Text = task.Title;
                StartTimeTextBox.Text = task.StartTime.ToString(@"hh\:mm");
                // For one-time tasks, if EndTime equals StartTime, leave EndTime empty.
                EndTimeTextBox.Text = (task.StartTime == task.EndTime) ? "" : task.EndTime.ToString(@"hh\:mm");
                NotesTextBox.Text = task.Notes;
                selectedColor = task.Color;
            }
            else
            {
                selectedColor = "Red";
            }
            // Ensure control templates are applied before updating borders.
            this.Loaded += TaskWindow_Loaded;
        }

        private void TaskWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateColorSelection();
        }

        private void UpdateColorSelection()
        {
            UpdateButtonBorder(btnColorRed);
            UpdateButtonBorder(btnColorYellow);
            UpdateButtonBorder(btnColorGreen);
            UpdateButtonBorder(btnColorBlue);
            UpdateButtonBorder(btnColorPurple);
        }

        private void UpdateButtonBorder(Button btn)
        {
            var border = btn.Template.FindName("colorBorder", btn) as Border;
            if (border != null)
            {
                border.BorderBrush = btn.Tag.ToString().Equals(selectedColor, StringComparison.OrdinalIgnoreCase)
                    ? Brushes.White
                    : Brushes.Transparent;
            }
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                selectedColor = btn.Tag.ToString();
                UpdateColorSelection();
            }
        }

        // NEW, simplified time parsing logic:
        // If the input contains "am" or "pm", use standard DateTime.TryParse.
        // Otherwise, assume 24-hour time.
        private bool TryParseTime(string input, out TimeSpan time)
        {
            time = TimeSpan.Zero;
            input = input.Trim().ToLower();
            if (input.Contains("am") || input.Contains("pm"))
            {
                // Use standard parsing.
                if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault, out DateTime dt))
                {
                    time = dt.TimeOfDay;
                    return true;
                }
                return false;
            }
            // Remove any "h" characters.
            input = input.Replace("h", "");
            // If input contains a colon, try standard parsing.
            if (input.Contains(":"))
            {
                return TimeSpan.TryParse(input, out time);
            }
            // If input is all digits, assume it's an hour or hour+minute in 24-hour time.
            if (input.All(char.IsDigit))
            {
                if (input.Length <= 2)
                {
                    // e.g. "8" or "08" -> 08:00
                    int hr = int.Parse(input);
                    time = TimeSpan.FromHours(hr);
                    return true;
                }
                else if (input.Length == 3)
                {
                    // e.g. "800" -> "08:00"
                    input = "0" + input; // e.g., "0800"
                    input = input.Insert(2, ":");
                    return TimeSpan.TryParse(input, out time);
                }
                else if (input.Length == 4)
                {
                    // e.g. "2000" -> "20:00"
                    input = input.Insert(2, ":");
                    return TimeSpan.TryParse(input, out time);
                }
            }
            return false;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string startInput = StartTimeTextBox.Text;
            if (string.IsNullOrWhiteSpace(startInput))
            {
                MessageBox.Show("Please enter a valid start time.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // If End Time is blank, treat as one-time task.
            string endInput = string.IsNullOrWhiteSpace(EndTimeTextBox.Text) ? startInput : EndTimeTextBox.Text;
            if (TryParseTime(startInput, out TimeSpan start) &&
                TryParseTime(endInput, out TimeSpan end))
            {
                // If end time is earlier than start time (and not equal), error.
                if (end < start && end != start)
                {
                    MessageBox.Show("End time cannot be earlier than start time.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (Task == null)
                    Task = new TaskItem();
                Task.Title = TitleTextBox.Text;
                Task.StartTime = start;
                Task.EndTime = end; // For one-time tasks, start==end.
                Task.Notes = NotesTextBox.Text;
                Task.Color = selectedColor;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Please enter valid time formats.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}