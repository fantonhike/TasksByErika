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
                // For one-time tasks, leave End Time empty.
                EndTimeTextBox.Text = (task.StartTime == task.EndTime) ? "" : task.EndTime.ToString(@"hh\:mm");
                NotesTextBox.Text = task.Notes;
                selectedColor = task.Color;
            }
            else
            {
                selectedColor = "Red";
            }
            // Ensure the templates have been applied before updating the border.
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

        private bool TryParseTime(string input, out TimeSpan time)
        {
            time = TimeSpan.Zero;
            input = input.Trim().ToLower();
            // New logic: if input contains "h", remove it.
            bool hasH = input.Contains("h");
            if (hasH)
            {
                input = input.Replace("h", "");
            }
            // If input is all digits:
            if (input.All(char.IsDigit))
            {
                // If no "h", assume that a one- or two-digit number means PM (if less than 12, add 12)
                if (!hasH)
                {
                    if (input.Length <= 2)
                    {
                        int hr = int.Parse(input);
                        if (hr < 12)
                            hr += 12;
                        input = hr.ToString() + ":00";
                    }
                    else if (input.Length == 3)
                    {
                        input = "0" + input;
                        if (input.Length == 4)
                        {
                            input = input.Insert(2, ":");
                        }
                    }
                    else if (input.Length == 4)
                    {
                        input = input.Insert(2, ":");
                    }
                }
                else
                {
                    // If "h" was present, treat input as literal (e.g. "1" means 1:00, "1h30" becomes "1:30")
                    if (input.Length <= 2)
                    {
                        input = input + ":00";
                    }
                    else if (input.Length == 3)
                    {
                        input = "0" + input;
                        input = input.Insert(2, ":");
                    }
                    else if (input.Length == 4)
                    {
                        input = input.Insert(2, ":");
                    }
                }
            }
            if (TimeSpan.TryParse(input, out time))
                return true;
            DateTime dt;
            if (DateTime.TryParse(input, out dt))
            {
                time = dt.TimeOfDay;
                return true;
            }
            return false;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string startInput = StartTimeTextBox.Text;
            // If End Time is blank, treat it as a one-time task (EndTime equals StartTime)
            string endInput = string.IsNullOrWhiteSpace(EndTimeTextBox.Text) ? startInput : EndTimeTextBox.Text;
            if (TryParseTime(startInput, out TimeSpan start) &&
                TryParseTime(endInput, out TimeSpan end))
            {
                if (Task == null)
                    Task = new TaskItem();
                Task.Title = TitleTextBox.Text;
                Task.StartTime = start;
                Task.EndTime = end;  // For one-time tasks, start==end.
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