﻿using ClangPowerTools.Events;
using ClangPowerTools.MVVM;
using ClangPowerTools.MVVM.Commands;
using ClangPowerTools.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ClangPowerTools
{
  public class TidyChecksViewModel : INotifyPropertyChanged
  {
    #region Members

    public event PropertyChangedEventHandler PropertyChanged;

    private string checkSearch = string.Empty;
    private TidySettingsModel tidyModel;
    private TidyChecksView tidyChecksView;
    private TidyCheckModel selectedCheck = new TidyCheckModel();
    private List<TidyCheckModel> tidyChecksList = new List<TidyCheckModel>();
    private ICommand resetSearchCommand;

    #endregion

    #region Constructor

    public TidyChecksViewModel(TidyChecksView view)
    {
      tidyModel = SettingsProvider.TidySettingsModel;

      tidyChecksView = view;
      tidyChecksView.Closed += OnClosed;

      // Click event is used because the Check value is changed many time from the code
      // In this way we don't need to make more checks to see from where the Check event was triggered 
      tidyChecksView.SelectAllCheckBox.Click += (object sender, RoutedEventArgs e) =>
      {
        // Check event is triggered before Click event. 
        // IsChecked property will already have the new value when the Click event will happend 
        SelectOrDeselectAll(tidyChecksView.SelectAllCheckBox.IsChecked == true ? true : false);
      };

      InitializeChecks();
    }

    #endregion


    #region Properties

    public TidyChecksView TidyChecksView
    {
      get
      {
        return tidyChecksView;
      }
      set
      {
        InitializeChecks();
        tidyChecksView = value;
      }
    }

    public List<TidyCheckModel> TidyChecksList
    {
      get
      {
        List<TidyCheckModel> checks = string.IsNullOrEmpty(checkSearch) ? tidyChecksList :
          tidyChecksList.Where(e => e.Name.Contains(checkSearch, StringComparison.OrdinalIgnoreCase)).ToList();

        CollectionElementsCounter.Initialize(checks);
        CollectionElementsCounter.ButtonStateEvent += CheckSelectAllButton;

        SetStateForEnableDisableAllButton(checks);

        return checks;
      }
    }

    public string CheckSearch
    {
      get
      {
        return checkSearch;
      }
      set
      {
        checkSearch = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CheckSearch"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TidyChecksList"));
      }
    }

    public TidyCheckModel SelectedCheck
    {
      get
      {
        return selectedCheck;
      }
      set
      {
        selectedCheck = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedCheck"));
      }
    }

    public bool CanExecute
    {
      get
      {
        return true;
      }
    }

    #endregion


    #region Commands

    public ICommand ResetSearchCommand
    {
      get => resetSearchCommand ?? (resetSearchCommand = new RelayCommand(() => ResetSearchField(), () => CanExecute));
    }

    #endregion


    #region Methods

    private string GetSelectedChecks()
    {
      var checks = new StringBuilder();

      foreach (TidyCheckModel item in tidyChecksList)
      {
        if (item.IsChecked)
        {
          checks.Append(item.Name).Append(";");
        }
      }

      if (checks.Length != 0)
        checks.Length--;

      return checks.ToString();
    }

    private void TickPredefinedChecks()
    {
      string input = SettingsProvider.TidySettingsModel.PredefinedChecks;
      input = Regex.Replace(input, @"\s+", "");
      input = input.Remove(input.Length - 1, 1);
      var checkNames = input.Split(';').ToList();

      foreach (string check in checkNames)
      {
        foreach (TidyCheckModel tidyModel in tidyChecksList)
        {
          if (string.Equals(check, tidyModel.Name, StringComparison.OrdinalIgnoreCase))
          {
            tidyModel.IsChecked = true;
          }
        }
      }
    }

    private void InitializeChecks()
    {
      string predefinedChecks = SettingsProvider.TidySettingsModel.PredefinedChecks;

      if (string.IsNullOrEmpty(predefinedChecks))
      {
        var tidyChecks = new TidyChecks();
        tidyChecksList = new List<TidyCheckModel>(tidyChecks.Checks);
      }
      else
      {
        var tidyChecksClean = new TidyChecksClean();
        tidyChecksList = new List<TidyCheckModel>(tidyChecksClean.Checks);
        TickPredefinedChecks();
      }

      SetStateForEnableDisableAllButton(tidyChecksList);
    }

    private void SelectOrDeselectAll(bool value)
    {
      var checks = string.IsNullOrEmpty(checkSearch) ? tidyChecksList :
          tidyChecksList.Where(e => e.Name.Contains(checkSearch, StringComparison.OrdinalIgnoreCase)).ToList();


      for (int i = 0; i < checks.Count; ++i)
      {
        checks[i].IsChecked = value;
      }
    }

    /// <summary>
    /// Set the state for Enable/Disable All button
    /// </summary>
    /// <param name="checks">Tidy checks collection</param>
    private void SetStateForEnableDisableAllButton(IEnumerable<TidyCheckModel> checks)
    {
      // to avoid enter in the second condition the first one must be split in two if statements
      // uncheck the Enable All button if the retured list of checks has 0 elements
      if (checks.Count() == 0)
      {
        if (tidyChecksView.SelectAllCheckBox.IsChecked == true)
          tidyChecksView.SelectAllCheckBox.IsChecked = false;
      }

      // check the Enable All button if all the checks from the current view are enabled
      else if (tidyChecksView.SelectAllCheckBox.IsChecked == false && !checks.Any(c => c.IsChecked == false))
      {
        tidyChecksView.SelectAllCheckBox.IsChecked = true;
      }

      // uncheck the Enable All button if any check from the list is disabled
      else if (tidyChecksView.SelectAllCheckBox.IsChecked == true && checks.Any(c => c.IsChecked == false))
      {
        tidyChecksView.SelectAllCheckBox.IsChecked = false;
      }
    }

    private void CheckSelectAllButton(object sender, BoolEventArgs e)
    {
      tidyChecksView.SelectAllCheckBox.IsChecked = e.Value;
    }

    private void OnClosed(object sender, EventArgs e)
    {
      tidyModel.PredefinedChecks = GetSelectedChecks();
      tidyChecksView.Closed -= OnClosed;

      CollectionElementsCounter.ButtonStateEvent -= CheckSelectAllButton;
    }

    private void ResetSearchField()
    {
      CheckSearch = string.Empty;
    }

    #endregion
  }
}
