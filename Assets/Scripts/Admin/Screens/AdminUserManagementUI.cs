using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdminUserManagementUI : MonoBehaviour
{
    [Header("Header")]
    public Button backBtn;

    [Header("Search")]
    public TMP_InputField searchField;
    public Button addUserBtn;

    [Header("Stats")]
    public TMP_Text totalUsersText;
    public TMP_Text activeUsersText;
    public TMP_Text inactiveUsersText;

    [Header("User List")]
    public Transform userListParent;
    public GameObject userRowPrefab;
    public TMP_Text listTitleText;

    private List<StudentRecord> filteredStudents = new List<StudentRecord>();

    void Start()
    {
        backBtn.onClick.AddListener(() => AdminUIManager.Instance.GoBack());
        addUserBtn.onClick.AddListener(OnAddUser);
        searchField.onValueChanged.AddListener(OnSearchChanged);

        if (AdminSessionManager.Instance != null)
            AdminSessionManager.Instance.OnDataRefreshed += Refresh;
    }

    void OnEnable()
    {
        AdminSessionManager.Instance?.LoadStudents();
        Refresh();
    }

    public void Refresh()
    {
        var session = AdminSessionManager.Instance;
        if (session == null) return;

        filteredStudents = new List<StudentRecord>(session.students);
        UpdateStats(session.students);
        BuildUserList(filteredStudents);
    }

    void UpdateStats(List<StudentRecord> all)
    {
        int active = all.FindAll(s => s.isActive).Count;
        int inactive = all.Count - active;
        if (totalUsersText) totalUsersText.text = all.Count.ToString();
        if (activeUsersText) activeUsersText.text = active.ToString();
        if (inactiveUsersText) inactiveUsersText.text = inactive.ToString();
    }

    void OnSearchChanged(string query)
    {
        var all = AdminSessionManager.Instance?.students ?? new List<StudentRecord>();
        if (string.IsNullOrEmpty(query))
        {
            filteredStudents = new List<StudentRecord>(all);
        }
        else
        {
            query = query.ToLower();
            filteredStudents = all.FindAll(s =>
                s.fullName.ToLower().Contains(query) ||
                s.email.ToLower().Contains(query));
        }
        BuildUserList(filteredStudents);
    }

    void BuildUserList(List<StudentRecord> students)
    {
        foreach (Transform child in userListParent) Destroy(child.gameObject);

        if (listTitleText)
            listTitleText.text = $"All Users ({students.Count})";

        foreach (var student in students)
        {
            var row = Instantiate(userRowPrefab, userListParent);
            row.GetComponent<UserRowUI>()?.Setup(student, OnToggleStatus, OnDeleteUser);
        }
    }

    void OnAddUser()
    {
        // Future: open "Add User" modal or send invite
        Debug.Log("[UserMgmt] Add User pressed.");
    }

    void OnToggleStatus(StudentRecord student, bool setActive)
    {
        AdminSessionManager.Instance?.ToggleStudentStatus(student.uid, setActive);
    }

    void OnDeleteUser(StudentRecord student)
    {
        // Show confirm modal before deleting
        AdminSessionManager.Instance?.DeleteStudent(student.uid);
    }

    void OnDestroy()
    {
        if (AdminSessionManager.Instance != null)
            AdminSessionManager.Instance.OnDataRefreshed -= Refresh;
    }
}
