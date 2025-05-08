using System.Collections;
using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine.UI;

public class FirebaseAuthManager : MonoBehaviour
{
    [Header("Firebase")]
    public DependencyStatus dependencyStatus;
    public static FirebaseAuthManager instance;

    public FirebaseAuth auth;
    public FirebaseUser user;

    public static string currentUsername = "";

    [Header("Login Fields")]
    public TMP_InputField emailLoginField;
    public TMP_InputField passwordLoginField;

    [Header("Register Fields")]
    public TMP_InputField nameRegisterField;
    public TMP_InputField emailRegisterField;
    public TMP_InputField passwordRegisterField;
    public TMP_InputField confirmPasswordRegisterField;

    private void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFirebase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                app.Options.DatabaseUrl = new System.Uri("https://login-c3529-default-rtdb.firebaseio.com/");

                auth = FirebaseAuth.DefaultInstance;

                Debug.Log("Firebase Initialized Successfully.");
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
            }
        });
    }

    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;

            if (!signedIn && user != null)
            {
                Debug.Log("Signed out " + user.UserId);
            }

            user = auth.CurrentUser;

            if (signedIn)
            {
                Debug.Log("Signed in " + user.UserId);
            }
        }
    }

    // ------------------ LOGIN ------------------
    public void Login()
    {
        StartCoroutine(LoginAsync(emailLoginField.text, passwordLoginField.text));
    }

    private IEnumerator LoginAsync(string email, string password)
    {
        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            Debug.LogError("Login failed: " + loginTask.Exception);
        }
        else
        {
            user = loginTask.Result.User;
            currentUsername = user.DisplayName;
            UIManager.Instance.OpenWelcomePanel();
        }
    }

    // ------------------ REGISTER ------------------
    public void Register()
    {
        StartCoroutine(RegisterAsync(
            nameRegisterField.text,
            emailRegisterField.text,
        
            passwordRegisterField.text,
            confirmPasswordRegisterField.text
        ));
    }

    private IEnumerator RegisterAsync(string name, string email, string password, string confirmPassword)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) )
        {
            Debug.LogError("Some fields are empty");
            yield break;
        }
        if (password != confirmPassword)
        {
            Debug.LogError("Passwords do not match");
            yield break;
        }

        if (auth == null) auth = FirebaseAuth.DefaultInstance;

        var registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => registerTask.IsCompleted);

        if (registerTask.Exception != null)
        {
            Debug.LogError(registerTask.Exception);
        }
        else
        {
            var createdUser = registerTask.Result.User;
            if (createdUser == null)
            {
                Debug.LogError("Created user is null!");
                yield break;
            }

            UserProfile profile = new UserProfile { DisplayName = name };
            var updateProfileTask = createdUser.UpdateUserProfileAsync(profile);
            yield return new WaitUntil(() => updateProfileTask.IsCompleted);

            if (updateProfileTask.Exception != null)
            {
                createdUser.DeleteAsync();
                Debug.LogError(updateProfileTask.Exception);
            }
            else
            {
                currentUsername = createdUser.DisplayName;

               

                UIManager.Instance.OpenLoginPanel();
            }
        }
    }

    
    // ------------------ FORGOT PASSWORD ------------------
public void ForgotPassword()
{
    StartCoroutine(ForgotPasswordAsync(emailLoginField.text));
}

private IEnumerator ForgotPasswordAsync(string email)
{
    if (string.IsNullOrEmpty(email))
    {
        Debug.LogError("Please enter your email to reset your password.");
        yield break;
    }

    var resetTask = auth.SendPasswordResetEmailAsync(email);
    yield return new WaitUntil(() => resetTask.IsCompleted);

    if (resetTask.Exception != null)
    {
        Debug.LogError("Password reset failed: " + resetTask.Exception);
    }
    else
    {
        Debug.Log("Password reset email sent successfully.");
        // Optional: Show popup or UI message
        UIManager.Instance.ShowPopup("Reset link sent to your email!");
    }
}

}