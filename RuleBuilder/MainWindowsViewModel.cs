using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RuleBuilder
{
    public class MainWindowsViewModel :ViewModelBase
    {
        private ObservableCollection<string> templates = new ObservableCollection<string>();
        private ObservableCollection<string> rules = new ObservableCollection<string>();
        private ObservableCollection<SignalDetail> signals = new ObservableCollection<SignalDetail>();

        private string selectedTemplate;
        private string selectedRule;
        private string inputRecord;

        public MainWindowsViewModel()
        {
            TemplatePath = ConfigurationManager.AppSettings.Get("templatePath");
            RulePath = ConfigurationManager.AppSettings.Get("rulePath");
            SignalDictionary = new Dictionary<string, SignalDetail>();
            LoadTemplates();
            LoadRules();
        }
        public IDictionary<string, SignalDetail> SignalDictionary { get; set; }

        public string RulePath { get; set; }

        public string TemplatePath { get; set; }

        public string InputRecord { get; set; }

        public string RuleRecord { get; set; }

        public List<SignalDetail> RuleList { get; set; }

        public ObservableCollection<string> Templates
        {
            get { return templates; }
            set { this.Set<ObservableCollection<string>>(() => Templates, ref templates, value); }
        }

        public ObservableCollection<string> Rules
        {
            get { return rules; }
            set { this.Set<ObservableCollection<string>>(() => Rules, ref rules, value); }
        }

        public ObservableCollection<SignalDetail> Signals
        {
            get { return signals; }
            set { this.Set<ObservableCollection<SignalDetail>>(() => Signals, ref signals, value); }
        }

        public string SelectedTemplate
        {
            get { return selectedTemplate; }
            set
            {
                this.Set<string>(() => SelectedTemplate, ref selectedTemplate, value);
                if (value != null)
                    LoadTemplateData();
            }
        }

        public string SelectedRule
        {
            get { return selectedRule; }
            set
            {
                this.Set<string>(() => SelectedRule, ref selectedRule, value);
                if (value != null)
                    LoadRule();
            }
        }

        public ICommand Filter { get { return new RelayCommand(OnFilter); } }

        public ICommand Reset { get { return new RelayCommand(OnReset); } }

        private void OnReset()
        {
            SignalDictionary.Clear();
            Signals.Clear();
            SelectedRule = null;
            SelectedTemplate = null;
        }

        private void OnFilter()
        {
            ProcessRule();
        }

        private void LoadTemplates()
        {
            List<string> temp = new List<string>();

            string[] XMLfiles = Directory.GetFiles(TemplatePath, "*.json");
            foreach (string file in XMLfiles)
            {
                temp.Add(Path.GetFileName(file));
            }
            Templates = new ObservableCollection<string>(temp);
        }


        private void LoadRules()
        {
            List<string> temp = new List<string>();

            string[] XMLfiles = Directory.GetFiles(RulePath, "*.json");
            foreach (string file in XMLfiles)
            {
                temp.Add(Path.GetFileName(file));
            }
            Rules = new ObservableCollection<string>(temp);
        }


        private void LoadTemplateData()
        {
            var getResult = new List<SignalDetail>();

            string fileName = TemplatePath + "\\" + selectedTemplate;
            using (StreamReader fileReader = new StreamReader(fileName))
            {
                InputRecord = fileReader.ReadToEnd();
            }

            getResult = JsonConvert.DeserializeObject<List<SignalDetail>>(InputRecord);


            foreach (var item in getResult)
            {
                if(!SignalDictionary.ContainsKey(item.Signal))
                SignalDictionary.Add(item.Signal, item);
            }
        }

        private void LoadRule()
        {
            string rulePath = RulePath + "\\" + selectedRule;
            using (StreamReader fileReader = new StreamReader(rulePath))
            {
                RuleRecord = fileReader.ReadToEnd();
            }

            RuleList = JsonConvert.DeserializeObject<List<SignalDetail>>(RuleRecord);
        }

        private void ProcessRule()
        {
            foreach (var item in RuleList)
            {
                if (!string.IsNullOrWhiteSpace(item.Rule))
                {
                    var value = item.Rule.Split(' ');
                    var last = value.LastOrDefault();

                    if (SignalDictionary.ContainsKey(item.Signal) && SignalDictionary[item.Signal] != null)
                    {
                        if (string.Compare(SignalDictionary[item.Signal].ValueType, "integer") == 0)
                        {
                            var itemValue = Convert.ToInt32(SignalDictionary[item.Signal].Value);
                            var ruleValue = Convert.ToInt32(last);

                            if ((item.Rule.Contains("greater than") && itemValue > ruleValue) ||
                                (item.Rule.Contains("less than") && itemValue < ruleValue) ||
                                item.Rule.Contains("equal to") && itemValue == ruleValue)
                            {
                                SignalDictionary[item.Signal].IsTrue = true;
                            }
                        }

                        if (string.Compare(SignalDictionary[item.Signal].ValueType, "string") == 0)
                        {
                            var itemValue = SignalDictionary[item.Signal].Value;
                            var ruleValue = last;

                            if (item.Rule.Contains("not be"))
                            {
                                if (!string.Equals(itemValue, ruleValue))
                                    SignalDictionary[item.Signal].IsTrue = true;
                            }
                            else
                            {
                                SignalDictionary[item.Signal].IsTrue = true;
                            }
                        }

                        if (string.Compare(SignalDictionary[item.Signal].ValueType, "Datetime") == 0)
                        {
                            var itemValue = Convert.ToDateTime(SignalDictionary[item.Signal].Value);
                            var now = DateTime.Now;
                            var result = DateTime.Compare(itemValue, now);

                            if (item.Rule.Contains("future") && result == 1 ||
                                item.Rule.Contains("past") && result == -1 ||
                                item.Rule.Contains("present") && result == 0)
                            {
                                SignalDictionary[item.Signal].IsTrue = true;

                            }
                        }
                    }
                }
            }
            foreach (var dictionaryValue in SignalDictionary)
            {
                if (dictionaryValue.Value.IsTrue)
                    Signals.Add(dictionaryValue.Value);
            }
        }
    }
}
