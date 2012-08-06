namespace RoliSoft.TVShowTracker.UserControls
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;

    using Parsers.WebSearch;
    using Parsers.WebSearch.Engines;

    using VistaControls.TaskDialog;

    /// <summary>
    /// Interaction logic for ProxiesSettings.xaml
    /// </summary>
    public partial class ProxiesSettings
    {
        /// <summary>
        /// Gets or sets the proxies list view item collection.
        /// </summary>
        /// <value>The proxies list view item collection.</value>
        public ObservableCollection<ProxiesListViewItem> ProxiesListViewItemCollection { get; set; }

        /// <summary>
        /// Gets or sets the proxied domains list view item collection.
        /// </summary>
        /// <value>The proxied domains list view item collection.</value>
        public ObservableCollection<ProxiedDomainsListViewItem> ProxiedDomainsListViewItemCollection { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxiesSettings"/> class.
        /// </summary>
        public ProxiesSettings()
        {
            InitializeComponent();
        }

        private bool _loaded;

        /// <summary>
        /// Handles the Loaded event of the UserControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void UserControlLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_loaded) return;

            try
            {
                ProxiesListViewItemCollection = new ObservableCollection<ProxiesListViewItem>();
                proxiesListView.ItemsSource = ProxiesListViewItemCollection;

                ProxiedDomainsListViewItemCollection = new ObservableCollection<ProxiedDomainsListViewItem>();
                proxiedDomainsListView.ItemsSource = ProxiedDomainsListViewItemCollection;

                ReloadProxies();
            }
            catch (Exception ex)
            {
                MainWindow.Active.HandleUnexpectedException(ex);
            }

            _loaded = true;

            ProxiesListViewSelectionChanged();
            ProxiedDomainsListViewSelectionChanged();
        }
        
        /// <summary>
        /// Handles the SelectionChanged event of the proxiesListView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void ProxiesListViewSelectionChanged(object sender = null, SelectionChangedEventArgs e = null)
        {
            if (!_loaded) return;

            proxyEditButton.IsEnabled = proxySearchButton.IsEnabled = proxyTestButton.IsEnabled = proxyRemoveButton.IsEnabled = proxiesListView.SelectedIndex != -1;
        }

        /// <summary>
        /// Handles the SelectionChanged event of the proxiedDomainsListView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void ProxiedDomainsListViewSelectionChanged(object sender = null, SelectionChangedEventArgs e = null)
        {
            if (!_loaded) return;

            proxyDomainEditButton.IsEnabled = proxyDomainRemoveButton.IsEnabled = proxiedDomainsListView.SelectedIndex != -1;
        }

        /// <summary>
        /// Reloads the proxy-related list views.
        /// </summary>
        private void ReloadProxies()
        {
            ProxiesListViewItemCollection.Clear();

            foreach (var proxy in Settings.Get<Dictionary<string, object>>("Proxies"))
            {
                ProxiesListViewItemCollection.Add(new ProxiesListViewItem
                    {
                        Name    = proxy.Key,
                        Address = (string)proxy.Value
                    });
            }

            ProxiesListViewSelectionChanged();

            ProxiedDomainsListViewItemCollection.Clear();

            foreach (var proxy in Settings.Get<Dictionary<string, object>>("Proxied Domains"))
            {
                ProxiedDomainsListViewItemCollection.Add(new ProxiedDomainsListViewItem
                    {
                        Icon   = "http://g.etfv.co/http://www." + proxy.Key + "?defaulticon=lightpng",
                        Domain = proxy.Key,
                        Proxy  = (string)proxy.Value
                    });
            }

            ProxiedDomainsListViewSelectionChanged();
        }

        /// <summary>
        /// Handles the Click event of the proxyAddButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxyAddButtonClick(object sender, RoutedEventArgs e)
        {
            new ProxyWindow().ShowDialog();
            ReloadProxies();
        }

        /// <summary>
        /// Handles the Click event of the proxyEditButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxyEditButtonClick(object sender, RoutedEventArgs e)
        {
            if (proxiesListView.SelectedIndex == -1) return;

            var sel = (ProxiesListViewItem)proxiesListView.SelectedItem;

            new ProxyWindow(sel.Name, sel.Address).ShowDialog();
            ReloadProxies();
        }

        /// <summary>
        /// Handles the Click event of the proxySearchButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxySearchButtonClick(object sender, RoutedEventArgs e)
        {
            if (proxiesListView.SelectedIndex == -1) return;
            
            Thread action = null;
            var done = false;

            var sel = (ProxiesListViewItem)proxiesListView.SelectedItem;
            var uri = new Uri(sel.Address.Replace("$domain.", string.Empty));

            if (uri.Host == "localhost" || uri.Host == "127.0.0.1" || uri.Host == "::1")
            {
                var app = "a local application";

                try
                {
                    var tcpRows = Utils.GetExtendedTCPTable();
                    foreach (var row in tcpRows)
                    {
                        if (((row.localPort1 << 8) + (row.localPort2) + (row.localPort3 << 24) + (row.localPort4 << 16)) == uri.Port)
                        {
                            app = "PID " + row.owningPid + " (" + Process.GetProcessById(row.owningPid).Modules[0].FileName + ")";
                            break;
                        }
                    }
                }
                catch { }

                new Thread(() => new TaskDialog
                    {
                        CommonIcon  = TaskDialogIcon.SecurityWarning,
                        Title       = sel.Name,
                        Instruction = "Potentially dangerous",
                        Content     = "This proxy points to a local loopback address on port " + uri.Port + ".\r\nYour requests will go to " + app + ", which will most likely forward them to an external server."
                    }.Show()).Start();
                return;
            }

            var td  = new TaskDialog
                {
                    Title           = sel.Name,
                    Instruction     = "Testing proxy",
                    Content         = "Testing whether " + uri.Host + " is a known proxy...",
                    CommonButtons   = TaskDialogButton.Cancel,
                    ShowProgressBar = true
                };
            td.ButtonClick += (s, v) =>
                {
                    if (!done)
                    {
                        try { action.Abort(); } catch { }
                    }
                };
            td.SetMarqueeProgressBar(true);
            new Thread(() => td.Show()).Start();

            action = new Thread(() =>
                {
                    try
                    {
                        var src = new DuckDuckGo();
                        var res = new List<SearchResult>();
                        res.AddRange(src.Search("\"" + uri.Host + "\" intitle:proxy"));

                        if (res.Count == 0)
                        {
                            res.AddRange(src.Search("\"" + uri.Host + "\" intitle:proxies"));
                        }

                        done = true;

                        if (td.IsShowing)
                        {
                            td.SimulateButtonClick(-1);
                        }

                        if (res.Count == 0)
                        {
                            new TaskDialog
                                {
                                    CommonIcon  = TaskDialogIcon.SecuritySuccess,
                                    Title       = sel.Name,
                                    Instruction = "Not a known public proxy",
                                    Content     = uri.Host + " does not seem to be a known public proxy." + Environment.NewLine + Environment.NewLine +
                                                  "If your goal is to trick proxy detectors, you're probably safe for now. However, you shouldn't use public proxies if you don't want to potentially compromise your account."
                                }.Show();
                            return;
                        }
                        else
                        {
                            new TaskDialog
                                {
                                    CommonIcon  = TaskDialogIcon.SecurityError,
                                    Title       = sel.Name,
                                    Instruction = "Known public proxy",
                                    Content     = uri.Host + " is a known public proxy according to " + new Uri(res[0].URL).Host.Replace("www.", string.Empty) + Environment.NewLine + Environment.NewLine +
                                                  "If the site you're trying to access through this proxy forbids proxy usage, they're most likely use a detection mechanism too, which will trigger an alert when it sees this IP address. Your requests will be denied and your account might also get banned. Even if the site's detector won't recognize it, using a public proxy is not such a good idea, because you could compromise your account as public proxy operators are known to be evil sometimes."
                                }.Show();
                            return;
                        }
                    }
                    catch (ThreadAbortException) { }
                    catch (Exception ex)
                    {
                        done = true;

                        if (td.IsShowing)
                        {
                            td.SimulateButtonClick(-1);
                        }

                        new TaskDialog
                            {
                                CommonIcon          = TaskDialogIcon.Stop,
                                Title               = sel.Name,
                                Instruction         = "Connection error",
                                Content             = "An error occured while checking the proxy.",
                                ExpandedControlText = "Show exception message",
                                ExpandedInformation = ex.Message
                            }.Show();
                    }
                });
            action.Start();
        }

        /// <summary>
        /// Handles the Click event of the proxyTestButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxyTestButtonClick(object sender, RoutedEventArgs e)
        {
            if (proxiesListView.SelectedIndex == -1) return;

            Thread action = null;
            var done = false;

            var sel = (ProxiesListViewItem)proxiesListView.SelectedItem;
            var uri = new Uri(sel.Address.Replace("$domain.", string.Empty));
            var td  = new TaskDialog
                {
                    Title           = sel.Name,
                    Instruction     = "Testing proxy",
                    Content         = "Testing connection through " + uri.Host + ":" + uri.Port + "...",
                    CommonButtons   = TaskDialogButton.Cancel,
                    ShowProgressBar = true
                };
            td.ButtonClick += (s, v) =>
                {
                    if (!done)
                    {
                        try { action.Abort(); } catch { }
                    }
                };
            td.SetMarqueeProgressBar(true);
            new Thread(() => td.Show()).Start();

            action = new Thread(() =>
                {
                    var s = Stopwatch.StartNew();

                    try
                    {
                        var b = Utils.GetHTML("http://rolisoft.net/b", proxy: sel.Address);
                        s.Stop();

                        done = true;

                        if (td.IsShowing)
                        {
                            td.SimulateButtonClick(-1);
                        }

                        var tor  = b.DocumentNode.SelectSingleNode("//img[@class='tor']");
                        var ip   = b.DocumentNode.GetTextValue("//span[@class='ip'][1]");
                        var host = b.DocumentNode.GetTextValue("//span[@class='host'][1]");
                        var geo  = b.DocumentNode.GetTextValue("//span[@class='geoip'][1]");

                        if (tor != null)
                        {
                            new TaskDialog
                                {
                                    CommonIcon  = TaskDialogIcon.SecurityError,
                                    Title       = sel.Name,
                                    Instruction = "TOR detected",
                                    Content     = ip + " is a TOR exit node." + Environment.NewLine + Environment.NewLine +
                                                  "If the site you're trying to access through this proxy forbids proxy usage, they're most likely use a detection mechanism too, which will trigger an alert when it sees this IP address. Your requests will be denied and your account might also get banned. Even if the site's detector won't recognize it, using TOR is not such a good idea, because you could compromise your account as TOR exit node operators are known to be evil sometimes."
                                }.Show();
                        }

                        if (ip == null)
                        {
                            new TaskDialog
                                {
                                    CommonIcon  = TaskDialogIcon.Stop,
                                    Title       = sel.Name,
                                    Instruction = "Proxy error",
                                    Content     = "The proxy did not return the requested resource, or greatly modified the structure of the page. Either way, it is not suitable for use with this software.",
                                }.Show();
                            return;
                        }

                        new TaskDialog
                            {
                                CommonIcon  = TaskDialogIcon.Information,
                                Title       = sel.Name,
                                Instruction = "Test results",
                                Content     = "Total time to get rolisoft.net/b: " + s.Elapsed + "\r\n\r\nIP address: " + ip + "\r\nHost name: " + host + "\r\nGeoIP lookup: " + geo,
                            }.Show();
                    }
                    catch (ThreadAbortException) { }
                    catch (Exception ex)
                    {
                        done = true;

                        if (td.IsShowing)
                        {
                            td.SimulateButtonClick(-1);
                        }

                        new TaskDialog
                            {
                                CommonIcon          = TaskDialogIcon.Stop,
                                Title               = sel.Name,
                                Instruction         = "Connection error",
                                Content             = "An error occured while connecting to the proxy.",
                                ExpandedControlText = "Show exception message",
                                ExpandedInformation = ex.Message
                            }.Show();
                    }
                    finally
                    {
                        if (s.IsRunning)
                        {
                            s.Stop();
                        }
                    }
                });
            action.Start();
        }

        /// <summary>
        /// Handles the Click event of the proxyRemoveButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxyRemoveButtonClick(object sender, RoutedEventArgs e)
        {
            if (proxiesListView.SelectedIndex == -1) return;

            var sel = (ProxiesListViewItem)proxiesListView.SelectedItem;

            if (MessageBox.Show("Are you sure you want to remove " + sel.Name + " and all the proxied domains associated with it?", "Remove " + sel.Name, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var dict = Settings.Get<Dictionary<string, object>>("Proxied Domains");

                foreach (var prdmn in dict.ToDictionary(k => k.Key, v => v.Value))
                {
                    if ((string)prdmn.Value == sel.Name)
                    {
                        dict.Remove(prdmn.Key);
                    }
                }

                Settings.Get<Dictionary<string, object>>("Proxies").Remove(sel.Name);
                Settings.Save();

                ReloadProxies();
            }
        }

        /// <summary>
        /// Handles the Click event of the proxyDomainAddButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxyDomainAddButtonClick(object sender, RoutedEventArgs e)
        {
            if (proxiesListView.Items.Count == 0)
            {
                MessageBox.Show("You need to add a new proxy before adding domains.", "No proxies", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            new ProxiedDomainWindow().ShowDialog();
            ReloadProxies();
        }

        /// <summary>
        /// Handles the Click event of the proxyDomainEditButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxyDomainEditButtonClick(object sender, RoutedEventArgs e)
        {
            if (proxiedDomainsListView.SelectedIndex == -1) return;

            if (proxiesListView.Items.Count == 0)
            {
                MessageBox.Show("You need to add a new proxy before adding domains.", "No proxies", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var sel = (ProxiedDomainsListViewItem)proxiedDomainsListView.SelectedItem;

            new ProxiedDomainWindow(sel.Domain, sel.Proxy).ShowDialog();
            ReloadProxies();
        }

        /// <summary>
        /// Handles the Click event of the proxyDomainRemoveButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ProxyDomainRemoveButtonClick(object sender, RoutedEventArgs e)
        {
            if (proxiedDomainsListView.SelectedIndex == -1) return;

            var sel = (ProxiedDomainsListViewItem)proxiedDomainsListView.SelectedItem;

            if (MessageBox.Show("Are you sure you want to remove " + sel.Domain + "?", "Remove " + sel.Domain, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Settings.Get<Dictionary<string, object>>("Proxied Domains").Remove(sel.Domain);
                Settings.Save();

                ReloadProxies();
            }
        }
    }
}
