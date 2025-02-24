using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using Newtonsoft.Json;

namespace OfflineCalendar
{
    public class TaskDay
    {
        public DateTime Date { get; set; }
        public List<TaskItem> Tasks { get; set; }
    }

    public class TaskItem
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }  // For one-time tasks, EndTime equals StartTime.
        public string Title { get; set; }
        public string Notes { get; set; }
        public string Color { get; set; } = "Green";
        public string TimeRange
        {
            get
            {
                DateTime startDT = DateTime.Today.Add(StartTime);
                DateTime endDT = DateTime.Today.Add(EndTime);
                if (StartTime == EndTime)
                    return $"{startDT.ToString("h:mmtt").ToLower()}";
                else
                    return $"{startDT.ToString("h:mmtt").ToLower()} - {endDT.ToString("h:mmtt").ToLower()}";
            }
        }
    }

    public partial class MainWindow : Window
    {
        Dictionary<DateTime, ObservableCollection<TaskItem>> tasksByDate = new Dictionary<DateTime, ObservableCollection<TaskItem>>();
        ObservableCollection<TaskItem> clipboardTasks = new ObservableCollection<TaskItem>();
        private DateTime currentWeekMonday;
        private string dataFile = "tasks.json";

        public MainWindow()
        {
            InitializeComponent();
        }

        private DateTime GetMonday(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            currentWeekMonday = GetMonday(DateTime.Today);
            PopulateWeekList();
            LoadTasks();
            foreach (ListBoxItem item in WeekListBox.Items)
            {
                if (item.Tag is DateTime day && day.Date == DateTime.Today)
                {
                    WeekListBox.SelectedItem = item;
                    break;
                }
            }
            if (WeekListBox.SelectedItem == null && WeekListBox.Items.Count > 0)
                WeekListBox.SelectedIndex = 0;
            TimelineCanvas.SizeChanged += (s, ev) => UpdateTimeline();
            UpdateTimeline();
        }

        private void PopulateWeekList()
        {
            WeekListBox.Items.Clear();
            for (int i = 0; i < 7; i++)
            {
                DateTime day = currentWeekMonday.AddDays(i);
                ListBoxItem item = new ListBoxItem
                {
                    Content = day.ToString("dddd, MMM dd"),
                    Tag = day,
                    Background = Brushes.Transparent
                };
                WeekListBox.Items.Add(item);
            }
        }

        private void RefreshTasksList(DateTime day)
        {
            if (tasksByDate.ContainsKey(day))
                TasksListView.ItemsSource = tasksByDate[day];
            else
                TasksListView.ItemsSource = null;
            UpdateTimeline();
        }

        private void WeekListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (WeekListBox.SelectedItem is ListBoxItem item && item.Tag is DateTime day)
                RefreshTasksList(day);
        }

        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            TaskWindow taskWindow = new TaskWindow();
            if (taskWindow.ShowDialog() == true)
            {
                DateTime selectedDay = (WeekListBox.SelectedItem as ListBoxItem)?.Tag as DateTime? ?? DateTime.Today;
                if (!tasksByDate.ContainsKey(selectedDay))
                    tasksByDate[selectedDay] = new ObservableCollection<TaskItem>();
                tasksByDate[selectedDay].Add(taskWindow.Task);
                tasksByDate[selectedDay] = new ObservableCollection<TaskItem>(
                    tasksByDate[selectedDay].OrderBy(t => t.StartTime));
                RefreshTasksList(selectedDay);
            }
        }

        private void EditTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (TasksListView.SelectedItem is TaskItem selectedTask)
            {
                TaskWindow taskWindow = new TaskWindow(selectedTask);
                if (taskWindow.ShowDialog() == true)
                {
                    DateTime selectedDay = (WeekListBox.SelectedItem as ListBoxItem)?.Tag as DateTime? ?? DateTime.Today;
                    tasksByDate[selectedDay] = new ObservableCollection<TaskItem>(
                        tasksByDate[selectedDay].OrderBy(t => t.StartTime));
                    RefreshTasksList(selectedDay);
                }
            }
            else
            {
                MessageBox.Show("Please select a task to edit.", "Edit Task", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void TasksListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (TasksListView.SelectedItem is TaskItem selectedTask)
            {
                EditTaskButton_Click(sender, e);
            }
        }

        private void CopyTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (TasksListView.SelectedItems.Count > 0)
            {
                clipboardTasks.Clear();
                foreach (var item in TasksListView.SelectedItems)
                {
                    if (item is TaskItem task)
                    {
                        clipboardTasks.Add(new TaskItem
                        {
                            Title = task.Title,
                            StartTime = task.StartTime,
                            EndTime = task.EndTime,
                            Notes = task.Notes,
                            Color = task.Color
                        });
                    }
                }
            }
        }

        private void PasteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (clipboardTasks.Any())
            {
                DateTime selectedDay = (WeekListBox.SelectedItem as ListBoxItem)?.Tag as DateTime? ?? DateTime.Today;
                if (!tasksByDate.ContainsKey(selectedDay))
                    tasksByDate[selectedDay] = new ObservableCollection<TaskItem>();
                foreach (var task in clipboardTasks)
                {
                    tasksByDate[selectedDay].Add(new TaskItem
                    {
                        Title = task.Title,
                        StartTime = task.StartTime,
                        EndTime = task.EndTime,
                        Notes = task.Notes,
                        Color = task.Color
                    });
                }
                tasksByDate[selectedDay] = new ObservableCollection<TaskItem>(
                    tasksByDate[selectedDay].OrderBy(t => t.StartTime));
                RefreshTasksList(selectedDay);
            }
        }

        private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (TasksListView.SelectedItems.Count > 0)
            {
                DateTime selectedDay = (WeekListBox.SelectedItem as ListBoxItem)?.Tag as DateTime? ?? DateTime.Today;
                if (tasksByDate.ContainsKey(selectedDay))
                {
                    var itemsToRemove = TasksListView.SelectedItems.Cast<TaskItem>().ToList();
                    foreach (var task in itemsToRemove)
                        tasksByDate[selectedDay].Remove(task);
                    RefreshTasksList(selectedDay);
                }
            }
            else
            {
                MessageBox.Show("Please select one or more tasks to delete.", "Delete Task", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClearWeekButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 7; i++)
            {
                DateTime day = currentWeekMonday.AddDays(i);
                if (tasksByDate.ContainsKey(day))
                    tasksByDate.Remove(day);
            }
            if (WeekListBox.SelectedItem is ListBoxItem item && item.Tag is DateTime selectedDay)
                RefreshTasksList(selectedDay);
        }

        private void PreviousWeekButton_Click(object sender, RoutedEventArgs e)
        {
            currentWeekMonday = currentWeekMonday.AddDays(-7);
            PopulateWeekList();
            WeekListBox.SelectedIndex = 0;
        }

        private void NextWeekButton_Click(object sender, RoutedEventArgs e)
        {
            currentWeekMonday = currentWeekMonday.AddDays(7);
            PopulateWeekList();
            WeekListBox.SelectedIndex = 0;
        }

        private void SaveTasks()
        {
            try
            {
                List<TaskDay> list = new List<TaskDay>();
                foreach (var kvp in tasksByDate)
                    list.Add(new TaskDay { Date = kvp.Key, Tasks = kvp.Value.ToList() });
                string json = JsonConvert.SerializeObject(list, Formatting.Indented);
                File.WriteAllText(dataFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving tasks: {ex.Message}");
            }
        }

        private void LoadTasks()
        {
            try
            {
                if (File.Exists(dataFile))
                {
                    string json = File.ReadAllText(dataFile);
                    List<TaskDay> list = JsonConvert.DeserializeObject<List<TaskDay>>(json);
                    tasksByDate.Clear();
                    foreach (var taskDay in list)
                        tasksByDate[taskDay.Date.Date] = new ObservableCollection<TaskItem>(taskDay.Tasks);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tasks: {ex.Message}");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveTasks();
        }

        private void TasksListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (TasksListView.View is GridView gridView && notesColumn != null)
            {
                double fixedWidth = 38 + 120 + 110;
                double newWidth = TasksListView.ActualWidth - fixedWidth - 35;
                if (newWidth < 100) newWidth = 100;
                notesColumn.Width = newWidth;
            }
        }

        private void UpdateTimeline()
        {
            TimelineCanvas.Children.Clear();
            double canvasWidth = TimelineCanvas.ActualWidth;
            double canvasHeight = TimelineCanvas.ActualHeight;
            if (canvasWidth == 0) canvasWidth = TimelineCanvas.RenderSize.Width;
            if (canvasWidth == 0) return;
            // Draw tick marks every 3 hours from 6 to 24.
            for (int hour = 6; hour <= 24; hour += 3)
            {
                double x = ((hour - 6) / 18.0) * canvasWidth;
                Line tick = new Line
                {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = canvasHeight,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1
                };
                TimelineCanvas.Children.Add(tick);
                if (hour != 24)
                {
                    string label = (hour == 12) ? "12pm" : (hour < 12 ? hour + "am" : (hour - 12) + "pm");
                    TextBlock textBlock = new TextBlock
                    {
                        Text = label,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888888")),
                        FontSize = 10
                    };
                    // Move the label slightly down.
                    Canvas.SetLeft(textBlock, x + 2);
                    Canvas.SetTop(textBlock, canvasHeight - 15);
                    TimelineCanvas.Children.Add(textBlock);
                }
            }
            DateTime selectedDay = (WeekListBox.SelectedItem as ListBoxItem)?.Tag as DateTime? ?? DateTime.Today;
            if (!tasksByDate.ContainsKey(selectedDay)) return;
            var tasks = tasksByDate[selectedDay];
            var sortedTasks = tasks.OrderBy(t => t.StartTime).ToList();
            // For overlap calculations: for one-time tasks, treat them as having a 20-minute effective range (10 minutes before and 10 minutes after).
            List<TimeSpan> rowEndTimes = new List<TimeSpan>();
            Dictionary<TaskItem, int> taskRow = new Dictionary<TaskItem, int>();
            foreach (var task in sortedTasks)
            {
                TimeSpan effectiveStart = task.StartTime;
                TimeSpan effectiveEnd = task.EndTime;
                if (task.StartTime == task.EndTime)
                {
                    effectiveStart = task.StartTime - TimeSpan.FromMinutes(10);
                    effectiveEnd = task.EndTime + TimeSpan.FromMinutes(10);
                    if (effectiveStart < TimeSpan.FromHours(6)) effectiveStart = TimeSpan.FromHours(6);
                    if (effectiveEnd > TimeSpan.FromHours(24)) effectiveEnd = TimeSpan.FromHours(24);
                }
                int row = 0;
                bool placed = false;
                for (; row < rowEndTimes.Count; row++)
                {
                    if (effectiveStart >= rowEndTimes[row])
                    {
                        rowEndTimes[row] = effectiveEnd;
                        taskRow[task] = row;
                        placed = true;
                        break;
                    }
                }
                if (!placed)
                {
                    rowEndTimes.Add(effectiveEnd);
                    taskRow[task] = rowEndTimes.Count - 1;
                }
            }
            double barHeight = 11;
            double verticalSpacing = 4;
            ColorToBrushConverter converter = new ColorToBrushConverter();
            foreach (var task in sortedTasks)
            {
                double startHour = task.StartTime.TotalHours;
                double endHour = task.EndTime.TotalHours;
                if (endHour <= 6 || startHour >= 24)
                    continue;
                startHour = Math.Max(startHour, 6);
                endHour = Math.Min(endHour, 24);
                double x = ((startHour - 6) / 18.0) * canvasWidth;
                double width = ((endHour - startHour) / 18.0) * canvasWidth;
                int row = taskRow[task];
                double y = row * (barHeight + verticalSpacing);
                // For one-time tasks, draw as a circle.
                if (task.StartTime == task.EndTime)
                {
                    width = barHeight;
                    double centerX = ((task.StartTime.TotalHours - 6) / 18.0) * canvasWidth;
                    x = centerX - width / 2;
                }
                Rectangle rect = new Rectangle
                {
                    Width = width,
                    Height = barHeight,
                    RadiusX = 6,
                    RadiusY = 6,
                    Fill = (Brush)converter.Convert(task.Color, typeof(Brush), null, System.Globalization.CultureInfo.CurrentCulture)
                };
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
                TimelineCanvas.Children.Add(rect);
            }
        }
    }

    public class ColorToBrushConverter : System.Windows.Data.IValueConverter
    {
        // Custom mapping: Red: #e67c73, Yellow: #f7cb4d, Green: #41b375, Blue: #7baaf7, Purple: #ba67c8
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string colorStr = value as string;
            if (!string.IsNullOrEmpty(colorStr))
            {
                switch (colorStr.ToLower())
                {
                    case "red":
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e67c73"));
                    case "yellow":
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f7cb4d"));
                    case "green":
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#41b375"));
                    case "blue":
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7baaf7"));
                    case "purple":
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ba67c8"));
                    default:
                        try { return (Brush)new BrushConverter().ConvertFromString(colorStr); }
                        catch { }
                        break;
                }
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}