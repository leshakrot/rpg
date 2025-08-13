using UnityEngine;
using Unity.Cinemachine;

namespace RPG.Core
{
    public class FollowCamera : MonoBehaviour
    {
        [Tooltip("����, �� ������� ������� ������")]
        public Transform target;

        [Tooltip("����, ������� ������ ����������� ����������� ��� ����������� � ���������")]
        public LayerMask layersToTransparent;

        [Tooltip("��������/��������� ������ ������������")]
        [SerializeField] private bool fadeObjects;

        [Tooltip("������� ������������ (0 - ��������� ����������, 1 - ��������� ������������)")]
        [Range(0f, 1f)]
        [SerializeField] private float alpha = 0.3f;

        [Tooltip("�������� �������� ������ �� �����������")]
        [SerializeField] private float rotationSpeed = 5f;

        [Tooltip("����������� ���� ������� ������ (� ��������)")]
        [SerializeField] private float minVerticalAngle = -45f;

        [Tooltip("������������ ���� ������� ������ (� ��������)")]
        [SerializeField] private float maxVerticalAngle = 45f;

        private CinemachineFreeLook freeLookCamera;
        private Transform prevHit; // ���������� ������, ������� ���� ����������
        private Renderer prevRenderer; // ������������ Renderer ����������� �������
        private Color originalColor; // �������� ���� ���������
        private int layerMaskValue; // �������� ������� ����� ��� �����

        private void Start()
        {
            if (target == null)
            {
                Debug.LogError("Target is not assigned! Please assign a target in the Inspector.");
                return;
            }

            // ����������� LayerMask � ������������� �������� ��� �������� ���������
            layerMaskValue = layersToTransparent.value;

            // ������� ��������� CinemachineFreeLook �� ������
            freeLookCamera = GetComponent<CinemachineBrain>()?.ActiveVirtualCamera as CinemachineFreeLook;
            if (freeLookCamera == null)
            {
                Debug.LogError("CinemachineFreeLook camera not found! Please add a CinemachineFreeLook component to your camera.");
            }
        }

        private void Update()
        {
            HandleMouseOrbit();
            HandleTransparency();
        }

        private void HandleMouseOrbit()
        {
            if (freeLookCamera == null || !Input.GetMouseButton(1)) // ���������, ������ �� ���
                return;

            // �������� ������� ������ �� ����
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

            // ��������� ���� �������� Cinemachine FreeLook
            freeLookCamera.m_YAxis.Value -= mouseY * Time.deltaTime; // ������������ ����
            freeLookCamera.m_XAxis.Value += mouseX * Time.deltaTime; // �������������� ����

            // ������������ ������������ ����
            freeLookCamera.m_YAxis.Value = Mathf.Clamp(freeLookCamera.m_YAxis.Value, minVerticalAngle / 90f, maxVerticalAngle / 90f);
        }

        private void HandleTransparency()
        {
            if (!fadeObjects || target == null)
                return;

            // ������� ��� �� ������ � ����
            Ray ray = new Ray(transform.position, (target.position - transform.position).normalized);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMaskValue))
            {
                Transform objectHit = hit.transform;

                // ���������, ����������� �� ������ � ������ �����
                if (((1 << objectHit.gameObject.layer) & layerMaskValue) != 0)
                {
                    // ���� ������ ���������� �� �����������
                    if (prevHit != objectHit)
                    {
                        RestorePreviousObject();

                        // ��������� ����� ������ � ��� Renderer
                        prevHit = objectHit;
                        prevRenderer = objectHit.GetComponent<Renderer>();

                        if (prevRenderer != null && prevRenderer.sharedMaterial != null)
                        {
                            // �������� �������� ����
                            originalColor = prevRenderer.sharedMaterial.color;
                            prevRenderer.material.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                        }
                    }
                }
                else
                {
                    RestorePreviousObject();
                }
            }
            else
            {
                RestorePreviousObject();
            }
        }

        private void RestorePreviousObject()
        {
            if (prevRenderer != null)
            {
                // ��������������� �������� ����
                prevRenderer.material.color = originalColor;

                // ������� ������
                prevHit = null;
                prevRenderer = null;
            }
        }

        private void OnDisable()
        {
            // ��� ���������� ������� ��������������� ��� �������
            RestorePreviousObject();
        }
    }
}