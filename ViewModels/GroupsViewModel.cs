using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SipCoreMobile.Data;
using SipCoreMobile.Data.Entities;
using SipCoreMobile.Models;
using SipCoreMobile.Services.Sip;

namespace SipCoreMobile.ViewModels;

/// <summary>
/// Port of the group-related slice of MainActivity.kt: loadGroups(), openGroup(),
/// createGroup(), addContactToGroup(), removeContactFromGroup(), callGroup(),
/// sendGroupMessage(). Feature-scoped per the same pattern as ChatViewModel (Batch 6).
/// </summary>
public partial class GroupsViewModel : ObservableObject
{
    private readonly ISipCoreRepository _repository;
    private readonly AppStateViewModel _appState;

    public ObservableCollection<GroupUi> Groups { get; } = new();
    public ObservableCollection<GroupMemberUi> SelectedGroupMembers { get; } = new();

    [ObservableProperty] private long selectedGroupId;
    [ObservableProperty] private string selectedGroupName = "";

    public GroupsViewModel(ISipCoreRepository repository, AppStateViewModel appState)
    {
        _repository = repository;
        _appState = appState;
    }

    public async Task LoadGroupsAsync()
    {
        try
        {
            var rows = await _repository.GetGroupsAsync();
            var items = new List<GroupUi>();

            foreach (var group in rows)
            {
                var members = await _repository.GetContactsInGroupAsync(group.Id);
                items.Add(new GroupUi { Id = group.Id, Name = group.Name, MemberCount = members.Count });
            }

            Groups.Clear();
            foreach (var g in items) Groups.Add(g);
        }
        catch
        {
            _appState.Status = "load groups failed";
        }
    }

    public async Task CreateGroupAsync(string name)
    {
        var cleanName = name.Trim();
        if (string.IsNullOrEmpty(cleanName)) return;

        try
        {
            await _repository.CreateGroupAsync(new ContactGroupEntity { Name = cleanName });
            await LoadGroupsAsync();
        }
        catch
        {
            _appState.Status = "create group failed";
        }
    }

    public async Task OpenGroupAsync(long groupId, string groupName)
    {
        try
        {
            SelectedGroupId = groupId;
            SelectedGroupName = groupName;

            var rows = await _repository.GetContactsInGroupAsync(groupId);
            var items = rows.Select(c => new GroupMemberUi { Extension = c.Extension, DisplayName = c.DisplayName });

            SelectedGroupMembers.Clear();
            foreach (var m in items) SelectedGroupMembers.Add(m);
        }
        catch
        {
            _appState.Status = "open group failed";
        }
    }

    public async Task AddMemberToGroupAsync(string extension)
    {
        try
        {
            var contact = _appState.Contacts.FirstOrDefault(c => c.Extension == extension);
            if (contact is not null)
            {
                await _repository.SaveLocalContactAsync(new LocalContactEntity
                {
                    Extension = contact.Extension,
                    DisplayName = contact.DisplayName
                });
            }

            await _repository.AddContactToGroupAsync(new ContactGroupMemberEntity { GroupId = SelectedGroupId, Extension = extension });
            await OpenGroupAsync(SelectedGroupId, SelectedGroupName);
        }
        catch
        {
            _appState.Status = "add member failed";
        }
    }

    public async Task RemoveMemberFromGroupAsync(string extension)
    {
        try
        {
            await _repository.RemoveContactFromGroupAsync(SelectedGroupId, extension);
            await OpenGroupAsync(SelectedGroupId, SelectedGroupName);
        }
        catch
        {
            _appState.Status = "remove member failed";
        }
    }

    /// <summary>
    /// Port of callGroup(): calls the first member, then adds the rest as conference
    /// participants with a short stagger (2.5s after the first call, 1.5s between each
    /// subsequent participant) -- matches the original's delay() calls exactly.
    /// </summary>
    public async Task CallGroupAsync(long groupId)
    {
        var members = (await _repository.GetContactsInGroupAsync(groupId)).ToList();
        if (members.Count == 0) return;

        _appState.ConferenceParticipants.Clear();

        var first = members[0];
        var firstDisplay = string.IsNullOrEmpty(first.DisplayName) ? first.Extension : $"{first.DisplayName} ({first.Extension})";
        _appState.ConferenceParticipants.Add(firstDisplay);

        await _appState.MakeOutgoingCallAsync(first.Extension);
        await Task.Delay(2500);

        foreach (var contact in members.Skip(1))
        {
            var displayName = string.IsNullOrEmpty(contact.DisplayName) ? contact.Extension : $"{contact.DisplayName} ({contact.Extension})";
            if (!_appState.ConferenceParticipants.Contains(displayName))
            {
                _appState.ConferenceParticipants.Add(displayName);
            }

            await _appState.SipManager.AddConferenceParticipantAsync(contact.Extension);
            await Task.Delay(1500);
        }
    }

    /// <summary>Port of sendGroupMessage(): sends the SIP MESSAGE individually to each member.</summary>
    public async Task SendGroupMessageAsync(long groupId, string text)
    {
        var members = await _repository.GetContactsInGroupAsync(groupId);
        foreach (var contact in members)
        {
            await _appState.SipManager.SendMessageAsync(contact.Extension, text);
        }
    }
}
