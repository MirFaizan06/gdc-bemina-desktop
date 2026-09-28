using CollegeAdmin.Contracts.Api;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>One row of the weekly timetable grid — every entry sharing a start time, bucketed by
/// day. A list (not a single nullable entry) per day because two different sections/programmes can
/// legitimately share a day+time slot in different rooms — the grid must not silently drop one.</summary>
public sealed record TimetableGridRow(
    string TimeLabel,
    IReadOnlyList<TimetableEntryDto> Mon,
    IReadOnlyList<TimetableEntryDto> Tue,
    IReadOnlyList<TimetableEntryDto> Wed,
    IReadOnlyList<TimetableEntryDto> Thu,
    IReadOnlyList<TimetableEntryDto> Fri,
    IReadOnlyList<TimetableEntryDto> Sat);
