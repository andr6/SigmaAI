﻿using AntDesign;
using AntDesign.ProLayout;
using Sigma.Core.Options;
using Sigma.Models;
using Sigma.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Sigma.Components
{
    public partial class RightContent: AntDomComponentBase
    {
        private CurrentUser _currentUser = new CurrentUser();
        private NoticeIconData[] _notifications = { };
        private NoticeIconData[] _messages = { };
        private NoticeIconData[] _events = { };
        private int _count = 0;

        private List<AutoCompleteDataItem<string>> DefaultOptions { get; set; } = new List<AutoCompleteDataItem<string>>
        {
            new AutoCompleteDataItem<string>
            {
                Label = "umi ui",
                Value = "umi ui"
            },
            new AutoCompleteDataItem<string>
            {
                Label = "Pro Table",
                Value = "Pro Table"
            },
            new AutoCompleteDataItem<string>
            {
                Label = "Pro Layout",
                Value = "Pro Layout"
            }
        };

        public AvatarMenuItem[] AvatarMenuItems { get; set; } = new AvatarMenuItem[]
        {
            new() { Key = "setting", IconType = "setting", Option = "Profile"},
            new() { IsDivider = true },
            new() { Key = "logout", IconType = "logout", Option = "Sign out"}
        };

        [Inject] protected NavigationManager NavigationManager { get; set; }

        [Inject] protected IUserService UserService { get; set; }
        [Inject] protected IProjectService ProjectService { get; set; }
        [Inject] protected MessageService MessageService { get; set; }

        [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; }
        [Inject] protected MessageService? Message { get; set; }

        //private ClaimsPrincipal context => ((SigmaAuthProvider)AuthenticationStateProvider).GetCurrentUser();

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            SetClassMap();
            _currentUser = await UserService.GetCurrentUserAsync();
            var notices = await ProjectService.GetNoticesAsync();
            _notifications = notices.Where(x => x.Type == "notification").Cast<NoticeIconData>().ToArray();
            _messages = notices.Where(x => x.Type == "message").Cast<NoticeIconData>().ToArray();
            _events = notices.Where(x => x.Type == "event").Cast<NoticeIconData>().ToArray();
            _count = notices.Length;
        }

        protected void SetClassMap()
        {
            ClassMapper
                .Clear()
                .Add("right");
        }

        public void HandleSelectUser(MenuItem item)
        {
            //switch (item.Key)
            //{
            //    case "setting":
            //        if (context.Identity.Name != LoginOption.User)
            //        {
            //            NavigationManager.NavigateTo("/setting/user/info/" + context.Identity.Name);
            //        }
            //        else
            //        {
            //            _ = Message.Info("Administrators do not need to configure", 2);
            //        }
            //        break;
            //    case "logout":
            //        NavigationManager.NavigateTo("/user/login");
            //        break;
            //}
        }

        public void HandleSelectLang(MenuItem item)
        {
        }

        public async Task HandleClear(string key)
        {
            switch (key)
            {
                case "notification":
                    _notifications = new NoticeIconData[] { };
                    break;
                case "message":
                    _messages = new NoticeIconData[] { };
                    break;
                case "event":
                    _events = new NoticeIconData[] { };
                    break;
            }
            await MessageService.Success($"Cleared {key}");
        }

        public async Task HandleViewMore(string key)
        {
            await MessageService.Info("Click on view more");
        }
    }
}