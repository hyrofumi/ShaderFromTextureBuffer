using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
	private RenderTexture buffer;
	private RenderTexture colorTexture;

	[SerializeField] private ComputeShader cs;

	int kernel;

	[SerializeField] private Shader shader;

	[SerializeField] private Material material;

	[SerializeField, Range(1, 1000)] private int length = 1000;

	/// <summary>
	/// 座標確認用
	/// </summary>
	[SerializeField] private RawImage image;

	/// <summary>
	/// 色確認用
	/// </summary>
	[SerializeField] private RawImage colorImage;

	[SerializeField] private Mesh instanceMesh;

	private Vector4[] positions;
	private ComputeBuffer positionBuffer;

	private Vector4[] colors;
	private ComputeBuffer colorBuffer;

	private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

	public int subMeshIndex = 0;
	private ComputeBuffer argsBuffer;

	private void Awake()
	{
		this.CreateBuffer(this.length);
	}

	void CreateBuffer(int length)
	{
		if (this.buffer == null)
		{
			this.buffer = new RenderTexture(length, 1, 16, RenderTextureFormat.ARGBFloat);
			this.buffer.enableRandomWrite = true;
			this.buffer.useMipMap = true;
			this.buffer.Create();

			if (this.image != null)
			{
				this.image.texture = this.buffer;
				this.image.rectTransform.sizeDelta = new Vector2(length, 10);
			}

			this.colorTexture = new RenderTexture(length, 1, 16, RenderTextureFormat.ARGBFloat);
			this.colorTexture.enableRandomWrite = true;
			this.colorTexture.useMipMap = true;
			this.colorTexture.Create();

			if (this.colorImage != null)
			{
				this.colorImage.texture = this.colorTexture;
				this.colorImage.rectTransform.sizeDelta = new Vector2(length, 10);
			}
		}



		this.subMeshIndex = Mathf.Clamp(this.subMeshIndex, 0, instanceMesh.subMeshCount - 1);

		this.positionBuffer = new ComputeBuffer(length * 4, sizeof(float));
		this.colorBuffer = new ComputeBuffer(length * 4, sizeof(float));

		this.positions = new Vector4[length];
		this.colors = new Vector4[length];

		for (int i = 0; i < length; i++)
		{
			float angle = Random.Range(0.0f, Mathf.PI * 2.0f);
			float distance = Random.Range(20.0f, 100.0f);
			float height = Random.Range(-2.0f, 2.0f);
			float size = Random.Range(0.05f, 0.25f);
			this.positions[i] = new Vector4(Mathf.Sin(angle) * distance, height, Mathf.Cos(angle) * distance, size);
			this.colors[i] = new Vector4(1, 1, 1, UnityEngine.Random.Range(0.0f, 1.0f));
		}

		this.positionBuffer.SetData(this.positions);
		this.colorBuffer.SetData(this.colors);

		this.argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
		this.args[0] = (uint)instanceMesh.GetIndexCount(subMeshIndex);
		this.args[1] = (uint)length;
		this.args[2] = (uint)instanceMesh.GetIndexStart(subMeshIndex);
		this.args[3] = (uint)instanceMesh.GetBaseVertex(subMeshIndex);
		this.argsBuffer.SetData(this.args);
	}

	void Start()
	{
		// this.buffer.Release();
		// this.buffer.enableRandomWrite = true;
		this.kernel = cs.FindKernel("calc");
		this.cs.SetTexture(this.kernel, "buffer", this.buffer);
		this.cs.SetBuffer(this.kernel, "PositionBuffer", this.positionBuffer);
		this.cs.SetVector("resolution", new Vector2(this.buffer.width, this.buffer.height));
	}

	void Update()
	{
		for (int i = 0; i < length; i++)
		{
			this.colors[i].w = Mathf.Abs(Mathf.Sin((float)i * 0.25f + Time.realtimeSinceStartup));
		}
		this.colorBuffer.SetData(this.colors);

		this.kernel = cs.FindKernel("calc");
		this.cs.SetFloat("time", Time.realtimeSinceStartup);
		this.cs.SetTexture(this.kernel, "buffer", this.buffer);
		this.cs.SetBuffer(this.kernel, "PositionBuffer", this.positionBuffer);
		this.cs.SetTexture(this.kernel, "colorTexture", this.colorTexture);
		this.cs.SetBuffer(this.kernel, "ColorBuffer", this.colorBuffer);
		this.cs.SetVector("resolution", new Vector2(this.buffer.width, this.buffer.height));
		uint x, y, z;
		this.cs.GetKernelThreadGroupSizes(this.kernel, out x, out y, out z);
		this.cs.Dispatch(this.kernel, this.length / 8, 1, 1);

		if (this.material != null)
		{
			this.material.SetTexture("_PositionTex", this.buffer);
			this.material.SetTexture("_ColorTex", this.colorTexture);
			this.material.SetInt("_InstanceNum", this.length);
			this.material.SetVector("_TextureSize", new Vector4(this.buffer.width, this.buffer.height, 1, 0));
			Graphics.DrawMeshInstancedIndirect(
				this.instanceMesh,
				this.subMeshIndex,
				this.material,
				new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)),
				this.argsBuffer
			);//, 0, null, UnityEngine.Rendering.ShadowCastingMode.On, true, 0);
		}

	}
}
