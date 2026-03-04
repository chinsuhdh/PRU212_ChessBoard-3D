using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {

    // ... Khai báo biến tốc độ, vùng biên ...
    public float verticalScrollArea = 10f;
	public float horizontalScrollArea = 10f;
	public float verticalScrollSpeed = 10f;
	public float horizontalScrollSpeed = 10f;
	

	// Chức năng: Bật/Tắt quyền điều khiển camera.
    // Dùng khi vào menu (tắt đi) hoặc vào game (bật lên).
    public void EnableControls(bool _enable) {
		
		if(_enable) {
			ZoomEnabled = true;
			MoveEnabled = true;
			CombinedMovement = true;
		} else {
			ZoomEnabled = false;
			MoveEnabled = false;
			CombinedMovement = false;
		}
	}
	
	public bool ZoomEnabled = true;
	public bool MoveEnabled = true;
	public bool CombinedMovement = true;
	
	private Vector2 _mousePos;
	private Vector3 _moveVector;
	private float _xMove;
	private float _yMove;
	private float _zMove;

    // Chức năng: Vòng lặp xử lý Input (Chuột/Phím) mỗi khung hình.
    // 1. Kiểm tra chuột có chạm cạnh màn hình không -> Di chuyển camera (RTS style).
    // 2. Kiểm tra phím WASD/Mũi tên -> Di chuyển camera.
    // 3. Kiểm tra con lăn chuột (ScrollWheel) -> Zoom lên/xuống (trục Y).
    // 4. Gọi hàm MoveMe để thực hiện di chuyển.
    void Update () {
		_mousePos = Input.mousePosition;
		
		//Move camera if mouse is at the edge of the screen
		if (MoveEnabled) {
			
			//Move camera if mouse is at the edge of the screen
			if (_mousePos.x < horizontalScrollArea)
			{
				_xMove = -1;
			}
			else if (_mousePos.x >= Screen.width - horizontalScrollArea) {
				_xMove = 1;
			}
			else {
				_xMove = 0;
			}
			
			if (_mousePos.y < verticalScrollArea) {
				_zMove = -1;
			}
			else if (_mousePos.y >= Screen.height - verticalScrollArea) {
				_zMove = 1;
			}
			else {
				_zMove = 0;
			}
			
			//Move camera if wasd or arrow keys are pressed
			float xAxisValue = Input.GetAxis("Horizontal");
			float zAxisValue = Input.GetAxis("Vertical");
			
			if (xAxisValue != 0) {
				if (CombinedMovement) {
					_xMove += xAxisValue;
				}
				else {
					_xMove = xAxisValue;
				}
			}
			
			if (zAxisValue != 0) {
				if (CombinedMovement) {
					_zMove += zAxisValue;
				}
				else {
					_zMove = zAxisValue;
				}
			}
			
		}
		else {
			_xMove = 0;
			_yMove = 0;
		}
		
		// Zoom Camera in or out
		if(ZoomEnabled) {
			if (Input.GetAxis("Mouse ScrollWheel") < 0) {
				_yMove = 1;
			}
			else if (Input.GetAxis("Mouse ScrollWheel") > 0) {
				_yMove = -1;
			}
			else {
				_yMove = 0;
			}
		}
		else {
			_zMove = 0;
		}
		
		//move the object
		MoveMe(_xMove, _yMove, _zMove);
	}

    // Chức năng: Thực hiện việc dịch chuyển (Translate) Transform của Camera.
    // x, z: Di chuyển ngang/dọc.
    // y: Di chuyển độ cao (Zoom).
    private void MoveMe(float x, float y, float z) {
		_moveVector = (new Vector3(x * horizontalScrollSpeed,
		                           y * verticalScrollSpeed, z * horizontalScrollSpeed) * Time.deltaTime);
		transform.Translate(_moveVector, Space.World);
	}
}