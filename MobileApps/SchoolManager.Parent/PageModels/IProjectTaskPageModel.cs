using CommunityToolkit.Mvvm.Input;
using SchoolManager.Parent.Models;

namespace SchoolManager.Parent.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}