using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FolderBrowserDialog.NetCore
{
    public class BrowseFolderControl : Control
    {
        private bool _disableSelectedPathEvents;
        private string _delayedSelectedPath;

        static BrowseFolderControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BrowseFolderControl), new FrameworkPropertyMetadata(typeof(BrowseFolderControl)));
        }

        public BrowseFolderControl()
        {
            CommandBindings.Add(new CommandBinding(AddNewFolderCommand, AddNewFolder_Executed, AddNewFolder_CanExecuted));

            Loaded += BrowseFolderControl_Loaded;

            //TODO: change this code by Action
            AddHandler(TreeViewItem.SelectedEvent, new RoutedEventHandler(OnSelected));
        }

        #region Dependency Properties

        public static readonly DependencyProperty SelectedPathProperty =
            DependencyProperty.Register(
                "SelectedPath",
                typeof(string),
                typeof(BrowseFolderControl),
                new PropertyMetadata(string.Empty, SelectedPath_Changed));

        public static readonly DependencyProperty RootFolderProperty =
            DependencyProperty.Register(
                "RootFolder",
                typeof(Environment.SpecialFolder),
                typeof(BrowseFolderControl),
                new PropertyMetadata(Environment.SpecialFolder.MyComputer, RootFolder_Changed));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(
                "Description",
                typeof(string),
                typeof(BrowseFolderControl));

        #endregion

        #region Routed Commands

        public static readonly RoutedUICommand AddNewFolderCommand =
            new RoutedUICommand("Add New Folder", "AddNewFolder", typeof(BrowseFolderControl));

        private void AddNewFolder_CanExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is string path && Directory.Exists(path);
        }

        private void AddNewFolder_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            FolderViewModel folderViewModel = (FolderViewModel)e.Parameter;

            if (!folderViewModel.IsFilled)
            {
                folderViewModel.IsExpanded = true;
                folderViewModel.Filled += AddNewFolderHandler();
                return;
            }

            AddNewFolder(folderViewModel);
        }

        private void AddNewFolderHandler(object sender, EventArgs e)
        {
            FolderViewModel folderViewModel = (FolderViewModel)sender;
            folderViewModel.Filled -= _addNewFolderHandler;

            AddNewFolder(folderViewModel);
        }

        private void AddNewFolder(FolderViewModel folderViewModel)
        {
            FolderViewModel newFolder = new FolderViewModel
            {
                IsNewFolder = true,
                IsSelected = true,
                Parent = folderViewModel,
            };

            folderViewModel.Subfolders.Add(newFolder);
        }

        #endregion

        #region Public Properties

        public FolderViewModel RootFolderViewModel
        {
            get { return (FolderViewModel)GetValue(RootFolderViewModelProperty); }
            private set { SetValue(RootFolderViewModelPropertyKey, value); }
        }

        public Environment.SpecialFolder RootFolder
        {
            get { return (Environment.SpecialFolder)GetValue(RootFolderProperty); }
            set { SetValue(RootFolderProperty, value); }
        }

        public string SelectedPath
        {
            get { return (string)GetValue(SelectedPathProperty); }
            set { SetValue(SelectedPathProperty, value); }
        }

        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        public FolderViewModel SelectedFolder
        {
            get { return (FolderViewModel)GetValue(SelectedFolderProperty); }
            private set { SetValue(SelectedFolderPropertyKey, value); }
        }

        #endregion

        #region Private Members

        #region Event Handlers

        private void OnSelected(object sender, RoutedEventArgs e)
        {
            TreeViewItem treeViewItem = (TreeViewItem)e.OriginalSource;
            SelectedFolder = (FolderViewModel)treeViewItem.DataContext;

            treeViewItem.Focus();
            //treeViewItem.pa


            Debug.Assert(!_disableSelectedPathEvents, "_disableSelectedPathEvents must be FALSE in OnSelected method.");

            _disableSelectedPathEvents = true;

            try
            {
                SelectedPath = SelectedFolder.FullName;
            }

            finally
            {
                _disableSelectedPathEvents = false;
            }
        }

        private void BrowseFolderControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (RootFolder == (Environment.SpecialFolder)RootFolderProperty.DefaultMetadata.DefaultValue)
            {
                UpdatePath(null);
            }
        }

        #endregion

        #region Depepndency Property Change Handlers

        private static void RootFolder_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BrowseFolderControl browseFolderControl = (BrowseFolderControl)d;

            IntPtr hWndOwner = browseFolderControl.GetControlWindowHandle();
            string path = FolderService.GetSpecialFolderPath(hWndOwner, (Environment.SpecialFolder)e.NewValue);

            browseFolderControl.UpdatePath(path);
        }

        private static void SelectedPath_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BrowseFolderControl)d).OnSelectedPathChanged();
        }

        #endregion

        #region Other

        private void UpdatePath(string path)
        {
            try
            {
                RootFolderViewModel = new FolderViewModel
                {
                    FullName = path,
                    Parent = null,
                };

                FolderService.StartFillFolderViewModel(RootFolderViewModel);

                if (string.IsNullOrEmpty(_delayedSelectedPath))
                {
                    return;
                }

                _delayedSelectedPath = null;
                OnSelectedPathChanged();
            }

            catch (Exception err)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                    Debug.WriteLine(err.Message);
                }

                throw;
            }
        }

        private void OnSelectedPathChanged()
        {
            if (_disableSelectedPathEvents || string.IsNullOrEmpty(SelectedPath))
            {
                return;
            }

            if (ReferenceEquals(RootFolderViewModel, null))
            {
                _delayedSelectedPath = SelectedPath;
                return;
            }

            FolderService.SelectPath(RootFolderViewModel, SelectedPath);
        }

        #endregion

        #endregion
    }
}