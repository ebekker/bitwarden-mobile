﻿using Bit.App.Resources;
using Bit.Core.Abstractions;
using Bit.Core.Enums;
using Bit.Core.Models.Domain;
using Bit.Core.Models.View;
using Bit.Core.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Bit.App.Pages
{
    public class GroupingsPageViewModel : BaseViewModel
    {
        private bool _loading = false;
        private bool _loaded = false;
        private bool _showAddCipherButton = false;
        private bool _showNoData = false;
        private bool _showList = false;
        private string _noDataText;
        private List<CipherView> _allCiphers;

        private readonly ICipherService _cipherService;
        private readonly IFolderService _folderService;
        private readonly ICollectionService _collectionService;
        private readonly ISyncService _syncService;

        public GroupingsPageViewModel()
        {
            _cipherService = ServiceContainer.Resolve<ICipherService>("cipherService");
            _folderService = ServiceContainer.Resolve<IFolderService>("folderService");
            _collectionService = ServiceContainer.Resolve<ICollectionService>("collectionService");
            _syncService = ServiceContainer.Resolve<ISyncService>("syncService");

            PageTitle = AppResources.MyVault;
            GroupedItems = new ExtendedObservableCollection<GroupingsPageListGroup>();
            LoadCommand = new Command(async () => await LoadAsync());
            AddCipherCommand = new Command(() => { /* TODO */ });
        }

        public bool ShowFavorites { get; set; } = true;
        public bool ShowFolders { get; set; } = true;
        public bool ShowCollections { get; set; } = true;
        public bool MainPage { get; set; }
        public CipherType? Type { get; set; }
        public string FolderId { get; set; }
        public string CollectionId { get; set; }

        public List<CipherView> Ciphers { get; set; }
        public List<CipherView> FavoriteCiphers { get; set; }
        public List<CipherView> NoFolderCiphers { get; set; }
        public List<FolderView> Folders { get; set; }
        public List<TreeNode<FolderView>> NestedFolders { get; set; }
        public List<Core.Models.View.CollectionView> Collections { get; set; }
        public List<TreeNode<Core.Models.View.CollectionView>> NestedCollections { get; set; }

        public bool Loading
        {
            get => _loading;
            set => SetProperty(ref _loading, value);
        }
        public bool Loaded
        {
            get => _loaded;
            set => SetProperty(ref _loaded, value);
        }
        public bool ShowAddCipherButton
        {
            get => _showAddCipherButton;
            set => SetProperty(ref _showAddCipherButton, value);
        }
        public bool ShowNoData
        {
            get => _showNoData;
            set => SetProperty(ref _showNoData, value);
        }
        public string NoDataText
        {
            get => _noDataText;
            set => SetProperty(ref _noDataText, value);
        }
        public bool ShowList
        {
            get => _showList;
            set => SetProperty(ref _showList, value);
        }
        public ExtendedObservableCollection<GroupingsPageListGroup> GroupedItems { get; set; }
        public Command LoadCommand { get; set; }
        public Command AddCipherCommand { get; set; }

        public async Task LoadAsync()
        {
            ShowNoData = false;
            Loading = true;
            ShowList = false;
            ShowAddCipherButton = true;
            var groupedItems = new List<GroupingsPageListGroup>();

            try
            {
                await LoadDataAsync();

                var favListItems = FavoriteCiphers?.Select(c => new GroupingsPageListItem { Cipher = c }).ToList();
                var ciphersListItems = Ciphers?.Select(c => new GroupingsPageListItem { Cipher = c }).ToList();
                var folderListItems = NestedFolders?.Select(f => new GroupingsPageListItem { Folder = f.Node }).ToList();
                var collectionListItems = NestedCollections?.Select(c =>
                    new GroupingsPageListItem { Collection = c.Node }).ToList();

                if(favListItems?.Any() ?? false)
                {
                    groupedItems.Add(new GroupingsPageListGroup(favListItems, AppResources.Favorites,
                        Device.RuntimePlatform == Device.iOS));
                }
                if(folderListItems?.Any() ?? false)
                {
                    groupedItems.Add(new GroupingsPageListGroup(folderListItems, AppResources.Folders,
                        Device.RuntimePlatform == Device.iOS));
                }
                if(collectionListItems?.Any() ?? false)
                {
                    groupedItems.Add(new GroupingsPageListGroup(collectionListItems, AppResources.Collections,
                        Device.RuntimePlatform == Device.iOS));
                }
                if(ciphersListItems?.Any() ?? false)
                {
                    groupedItems.Add(new GroupingsPageListGroup(ciphersListItems, AppResources.Items,
                        Device.RuntimePlatform == Device.iOS));
                }
                GroupedItems.ResetWithRange(groupedItems);
            }
            finally
            {
                ShowNoData = !groupedItems.Any();
                ShowList = !ShowNoData;
                Loaded = true;
                Loading = false;
            }
        }

        public async Task SelectCipherAsync(CipherView cipher)
        {
            var page = new ViewPage(cipher.Id);
            await Page.Navigation.PushModalAsync(new NavigationPage(page));
        }

        public async Task SelectFolderAsync(CipherType type)
        {
            string title = null;
            switch(Type.Value)
            {
                case CipherType.Login:
                    title = AppResources.Logins;
                    break;
                case CipherType.SecureNote:
                    title = AppResources.SecureNotes;
                    break;
                case CipherType.Card:
                    title = AppResources.Cards;
                    break;
                case CipherType.Identity:
                    title = AppResources.Identities;
                    break;
                default:
                    break;
            }
            var page = new GroupingsPage(false, type, null, null, title);
            await Page.Navigation.PushAsync(page);
        }

        public async Task SelectFolderAsync(FolderView folder)
        {
            var page = new GroupingsPage(false, null, folder.Id ?? "none", null, folder.Name);
            await Page.Navigation.PushAsync(page);
        }

        public async Task SelectCollectionAsync(Core.Models.View.CollectionView collection)
        {
            var page = new GroupingsPage(false, null, null, collection.Id, collection.Name);
            await Page.Navigation.PushAsync(page);
        }

        private async Task LoadDataAsync()
        {
            NoDataText = AppResources.NoItems;
            _allCiphers = await _cipherService.GetAllDecryptedAsync();
            if(MainPage)
            {
                if(ShowFolders)
                {
                    Folders = await _folderService.GetAllDecryptedAsync();
                    NestedFolders = await _folderService.GetAllNestedAsync();
                }
                if(ShowCollections)
                {
                    Collections = await _collectionService.GetAllDecryptedAsync();
                    NestedCollections = await _collectionService.GetAllNestedAsync(Collections);
                }

                foreach(var c in _allCiphers)
                {
                    if(c.Favorite)
                    {
                        if(FavoriteCiphers == null)
                        {
                            FavoriteCiphers = new List<CipherView>();
                        }
                        FavoriteCiphers.Add(c);
                    }
                    if(c.FolderId == null)
                    {
                        if(NoFolderCiphers == null)
                        {
                            NoFolderCiphers = new List<CipherView>();
                        }
                        NoFolderCiphers.Add(c);
                    }
                }
                FavoriteCiphers = _allCiphers.Where(c => c.Favorite).ToList();
            }
            else
            {
                if(Type != null)
                {
                    Ciphers = _allCiphers.Where(c => c.Type == Type.Value).ToList();
                }
                else if(FolderId != null)
                {
                    NoDataText = AppResources.NoItemsFolder;
                    FolderId = FolderId == "none" ? null : FolderId;
                    if(FolderId != null)
                    {
                        var folderNode = await _folderService.GetNestedAsync(FolderId);
                        if(folderNode?.Node != null)
                        {
                            PageTitle = folderNode.Node.Name;
                            NestedFolders = (folderNode.Children?.Count ?? 0) > 0 ? folderNode.Children : null;
                        }
                    }
                    else
                    {
                        PageTitle = AppResources.FolderNone;
                    }
                    Ciphers = _allCiphers.Where(c => c.FolderId == FolderId).ToList();
                }
                else if(CollectionId != null)
                {
                    ShowAddCipherButton = false;
                    NoDataText = AppResources.NoItemsCollection;
                    var collectionNode = await _collectionService.GetNestedAsync(CollectionId);
                    if(collectionNode?.Node != null)
                    {
                        PageTitle = collectionNode.Node.Name;
                    }
                    Ciphers = _allCiphers.Where(c => c.CollectionIds?.Contains(CollectionId) ?? false).ToList();
                }
                else
                {
                    PageTitle = AppResources.AllItems;
                    Ciphers = _allCiphers;
                }
            }
        }
    }
}