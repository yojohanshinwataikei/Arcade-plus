using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SelectionRendering : ScriptableRendererFeature
{
	class SelectionPass : ScriptableRenderPass
	{
		public static readonly int SampleDistanceShaderId = Shader.PropertyToID("_SampleDistance");
		public static readonly int BlitTextureSizeShaderId = Shader.PropertyToID("_BlitTextureSize");
		public static readonly int SelectionColorShaderId = Shader.PropertyToID("_SelectionColor");
		public static readonly int OutlineShaderId = Shader.PropertyToID("_OutlineColor");
		public SelectionRendering feature;
		private RTHandle selection;
		private RTHandle selectionDepth;
		private RTHandle cameraColor;
		private List<ShaderTagId> shaderTagIdList = new List<ShaderTagId> {
				new ShaderTagId("UniversalForward"),
				new ShaderTagId("UniversalForwardOnly"),
				new ShaderTagId("LightweightForward"),
				new ShaderTagId("SRPDefaultUnlit")
			};

		public Material selectionBlitMaterial;
		public Material selectionMaterial;

		public SelectionPass(SelectionRendering feature)
		{

			this.feature = feature;
			this.selectionMaterial = CoreUtils.CreateEngineMaterial(feature.SelectionShader);
			this.selectionBlitMaterial = CoreUtils.CreateEngineMaterial(feature.SelectionBlitShader);
			profilingSampler = new ProfilingSampler("SelectionPass");
		}

		public void SetTarget(RTHandle cameraColor)
		{
			this.cameraColor = cameraColor;
		}

		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
			RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
			descriptor.colorFormat = RenderTextureFormat.ARGB1555;
			RenderingUtils.ReAllocateIfNeeded(ref selectionDepth, descriptor, wrapMode: TextureWrapMode.Clamp, name: "_selection");
			descriptor.depthBufferBits = 0;
			RenderingUtils.ReAllocateIfNeeded(ref selection, descriptor, wrapMode: TextureWrapMode.Clamp, name: "_selection");

			selectionBlitMaterial.SetFloat(SampleDistanceShaderId, feature.SampleDistance * ((float)descriptor.width) / feature.SampleDistanceReferenceWidth);
			selectionBlitMaterial.SetVector(BlitTextureSizeShaderId, new Vector4(descriptor.width, descriptor.height));
			selectionBlitMaterial.SetColor(SelectionColorShaderId, feature.SelectionColor);
			selectionBlitMaterial.SetColor(OutlineShaderId, feature.OutlineColor);

			ConfigureTarget(selection, selectionDepth);
			ConfigureClear(ClearFlag.All, new Color(0, 0, 0, 0));
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (renderingData.cameraData.cameraType != CameraType.Game)
			{
				return;
			}
			CommandBuffer cmd = CommandBufferPool.Get(name: "Selection");
			using (new ProfilingScope(profilingSampler))
			{
				DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagIdList, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
				drawingSettings.overrideMaterial = selectionMaterial;
				RendererListParams rendererListParams = new RendererListParams(
					renderingData.cullResults,
					drawingSettings,
					new FilteringSettings(RenderQueueRange.all, -1, feature.RenderingLayerMask)
				);

				RendererList rendererList = context.CreateRendererList(ref rendererListParams);

				cmd.DrawRendererList(rendererList);
				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();
				Blitter.BlitCameraTexture(cmd, selection, cameraColor, selectionBlitMaterial, 0);

				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();
			}
			CommandBufferPool.Release(cmd);
		}

		public override void OnCameraCleanup(CommandBuffer cmd)
		{
		}
	}

	SelectionPass selectionPass;
	public Shader SelectionShader;
	public Shader SelectionBlitShader;
	public Color OutlineColor;
	public Color SelectionColor;
	public float SampleDistance;
	public float SampleDistanceReferenceWidth;

	// TODO: Good editor
	public uint RenderingLayerMask = uint.MaxValue;

	public override void Create()
	{
		if (SelectionShader == null)
		{
			SelectionShader = Shader.Find("SelectionRendering/Selection");
		}
		if (SelectionBlitShader == null)
		{
			SelectionShader = Shader.Find("SelectionRendering/SelectionBlit");
		}

		selectionPass = new SelectionPass(this);

		selectionPass.renderPassEvent = RenderPassEvent.AfterRendering;
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		renderer.EnqueuePass(selectionPass);
	}
	public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
	{
		if (renderingData.cameraData.cameraType == CameraType.Game)
		{
			selectionPass.SetTarget(renderer.cameraColorTargetHandle);
		}
	}
}


