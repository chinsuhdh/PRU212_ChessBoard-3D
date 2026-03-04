using System.Collections;
using Exploder.Utils;
using UnityEngine;
using UnityEngine.AI;

public class PiecePieces : MonoBehaviour
{
    //public Placement tilePlacement{ get; set; }
    public bool IsWhite { get; set; }

    private Animator _anim;
    private NavMeshAgent _navMeshAgent;
    private PieceManager _pieceManager;

    private AudioSource _audioWalk;
    private AudioSource _audioAttack;
    private AudioSource _audioGetHit;

    private bool _moving;
    private bool _attacking;
    private bool _attackAnimation;
    private bool _rock; // Biến kiểm tra nhập thành
    public bool _arrived { get; private set; } // Biến kiểm tra đã đến nơi chưa

    private Vector3 attackDestination;
    private GameObject _enemy;

    // Khởi tạo: Lấy các Component (Animator, NavMeshAgent, Audio) và cài đặt âm thanh
    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _navMeshAgent = gameObject.GetComponent<NavMeshAgent>();
        _pieceManager = GetComponentInParent<PieceManager>();

        var audioSources = GetComponentsInChildren<AudioSource>();
        foreach (var audioSource in audioSources)
        {
            audioSource.volume = 0.5f;
            audioSource.playOnAwake = false;
        }
        _audioWalk = audioSources[0];
        _audioWalk.loop = true;
        _audioAttack = audioSources[1];
        _audioGetHit = audioSources[2];
    }

    // Chức năng: Reset trạng thái của quân cờ (dừng di chuyển, xoay về hướng đúng)
    // Thường dùng khi khởi tạo lại bàn cờ
    public void ResetMovement()
    {
        _moving = false;
        _attacking = false;
        Rotate();
    }

    // Chức năng: Bắt đầu di chuyển quân cờ đến vị trí placement bằng NavMeshAgent
    public void Move(Vector3 placement, bool rock = false)
    {
        _arrived = false;
        _navMeshAgent.destination = placement;
        _moving = true;
        _audioWalk.Play();
        _rock = rock;
    }

    // Chức năng: Thiết lập thông số để bắt đầu tấn công (Di chuyển đến chỗ địch -> Ghi nhớ vị trí -> Đánh dấu là đang tấn công)
    public void Attack(Vector3 placement, Vector3 enemyPlacement, GameObject enemy)
    {
        Move(enemyPlacement); // Đi đến chỗ địch trước
        attackDestination = placement; // Lưu vị trí đích cuối cùng (sau khi giết địch sẽ đứng vào đây)
        _attacking = true;
        _enemy = enemy;
    }

    // Coroutine: Thực hiện chuỗi hoạt động tấn công theo kịch bản:
    // Chờ -> Đánh (Play sound) -> Địch bị đau -> Đánh tiếp -> Địch chết -> Quay về ô đích
    private IEnumerator AttackTarget()
    {
        _attackAnimation = true;
        yield return new WaitForSeconds(0.6f);
        _audioAttack.Play();
        StartCoroutine(_enemy.GetComponent<PiecePieces>().GetHurt()); // Gọi hàm bị đau của địch
        yield return new WaitForSeconds(0.5f);
        _attackAnimation = false;
        yield return new WaitForSeconds(0.1f);
        _attackAnimation = true;
        yield return new WaitForSeconds(0.6f);
        _audioAttack.Stop();
        _audioAttack.Play();
        StartCoroutine(_enemy.GetComponent<PiecePieces>().Die()); // Gọi hàm chết của địch
        _enemy = null;
        _attackAnimation = false;
        yield return new WaitForSeconds(1f);
        _attacking = false;
        Move(attackDestination); // Di chuyển vào ô của địch sau khi địch chết
    }

    // Coroutine: Xử lý cái chết của quân cờ
    // Chạy anim chết -> Kêu á á -> Tạo hiệu ứng nổ -> Ẩn quân cờ
    public IEnumerator Die() // <--- Đã đổi thành public để BoardManager gọi được
    {
        _anim.SetBool("Dead", true);
        _audioGetHit.Play();
        yield return new WaitForSeconds(1f);
        _audioGetHit.Stop();

        // Kiểm tra null để tránh lỗi nếu chưa gán Explosion
        if (_pieceManager != null && _pieceManager.explosion != null)
        {
            _pieceManager.explosion.transform.position = gameObject.transform.position;
            _pieceManager.explosion.GetComponent<AudioSource>().Play();
        }

        // Xử lý hiệu ứng nổ (Exploder) - Kiểm tra null cho Singleton
        if (Exploder.Utils.ExploderSingleton.Instance != null)
        {
            Exploder.Utils.ExploderSingleton.Instance.ExplodeCracked(gameObject);
        }

        gameObject.SetActive(false);
    }

    // Coroutine: Xử lý khi bị đánh (Chạy anim bị thương)
    private IEnumerator GetHurt()
    {
        _anim.SetTrigger("hitted");
        _audioGetHit.Play();
        yield return new WaitForSeconds(.1f);
    }

    // Chức năng: Xác định hướng xoay của quân cờ (Quân trắng quay xuôi, quân đen quay ngược 180 độ)
    private void Rotate()
    {
        StartCoroutine(IsWhite ? RotateSmooth(-transform.eulerAngles.y) : RotateSmooth(180 - transform.eulerAngles.y));
    }

    // Coroutine: Xoay quân cờ một cách mượt mà (Lerp) thay vì xoay cái rụp
    private IEnumerator RotateSmooth(float value)
    {
        if (value > 180) value -= 360;
        if (value < -180) value += 360;
        const float x = 0.05f;
        for (var i = 0; i < 20; i++)
        {
            transform.Rotate(0, Mathf.Lerp(0, value, x), 0);
            yield return new WaitForSeconds(0.01f);
        }
    }

    // Chức năng: Kết thúc lượt đi, báo cáo lại cho PieceManager
    private void TurnFinal()
    {
        _pieceManager.FinishedAnim();
        _arrived = true;
    }

    // Vòng lặp chính: Cập nhật Animator và kiểm tra logic di chuyển từng khung hình
    private void Update()
    {
        _anim.SetBool("moving", _moving);
        _anim.SetBool("attacking", _attackAnimation);

        // Nếu đang di chuyển và đã tìm được đường
        if (_moving && !_navMeshAgent.pathPending)
        {
            // Nếu đang đi đánh nhau và khoảng cách tới địch < 5 -> Bắt đầu đánh
            if (_attacking && _navMeshAgent.remainingDistance < 5)
            {
                StartCoroutine(AttackTarget());
                _moving = false;
                _navMeshAgent.SetDestination(transform.position); // Dừng lại để đánh
                _audioWalk.Stop();
            }
            // Nếu đi bình thường và đã đến rất gần đích (< 0.25) -> Dừng lại
            else if (_navMeshAgent.remainingDistance < 0.25)
            {
                _moving = false;
                _audioWalk.Stop();
                _navMeshAgent.SetDestination(transform.position);
                Rotate(); // Xoay lại cho ngay ngắn
                if (!_rock) TurnFinal(); // Kết thúc lượt (trừ khi đang nhập thành thì logic khác xử lý)
            }
        }
    }
}