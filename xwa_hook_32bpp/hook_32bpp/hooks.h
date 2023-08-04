#pragma once
#include "hook_function.h"

#include "32bpp.h"

static const HookFunction g_hookFunctions[] =
{
	{ 0x4CCB05, SetOptNameHook },
	{ 0x441DD0, SetAlphaMaskHook },
	{ 0x441B40, CreateLightMapHook },
	{ 0x441A07, ConvertColorMapHook },
	{ 0x4CE44C, DatImage32Hook },
	{ 0x4424CA, ComputeGlobalLightsHook },
	{ 0x4326E8, DatImageSetPixelFormatHook },
	{ 0x4CD061, OptCreateD3DTexturesFromTexturesHook },
	{ 0x4CD167, OptCreateD3DTexturesFromTexturesLoopHook },
	{ 0x4CD093, OptTransformTexturesToD3DTexturesHook },
	{ 0x4CD263, OptTransformTexturesToD3DTexturesLoopHook },
};

static const HookPatchItem g_setOptNamePatch[] =
{
	{ 0x0CBF00, "8B442404558B6C240C", "E82BC00D00C3909090" },
};

static const HookPatchItem g_setTexturesBpp8To32Patch[] =
{
	{ 0x1964C5, "8A028801", "8B028901" },
	{ 0x1964CF, "83C101", "83C104" },
	{ 0x1964DE, "83C201", "83C204" },
	{ 0x1955BE, "8B4804", "909090" },
	{ 0x1955C1, "898DE8FEFFFF83BDE8FEFFFF00751A", "C785E8FEFFFF00000000909090EB1A" },
	{ 0x195D30, "755E", "EB5E" },
	{ 0x040ED9, "8D040B", "8D0499" },
	{ 0x040F0F, "C7843C0806000008000000", "C7843C0806000020000000" },
	{ 0x040F1A, "74098D140B8B4C24208911", "C1A43CFC05000002909090" },
	{ 0x041064, "399424080B0000", "BE03000000EB56" },
	{ 0x198A2F, "8B484C83E120", "8B4864C1E918" },
	{ 0x198AED, "83E108", "83E128" },
	//{ 0x040F25, "8B4C243C85C9", "E9FF00000090" },
	{ 0x040F2B, "0F84F8000000", "909090909090" },
	{ 0x040E02, "7D0D6838475B00", "E819711600EB08" },
};

static const HookPatchItem g_setTextureAlphaMaskPatch[] =
{
	{ 0x081EA1, "8B45083BC20F84000100008B4D0C8B018B4814898EB4000000", "33C98A4802898EB4000000890D30CA6800E93E010000909090" },
	{ 0x081F8C, "8B45083BC274198B450C8B008B4814890D30CA6800", "33C08A41028986B4000000A330CA6800EB57909090" },
	{ 0x0411CB, "8A8C24080B0000", "E8606D16008BC8" },
	{ 0x0CC08F, "E8AC000000", "9090909090" },
};

static const HookPatchItem g_createLightMapPatch[] =
{
	{ 0x041007, "C7843CF800000008000000", "C7843CF800000020000000" },
	{ 0x041012, "740D68F8465B00E872880C0083C404", "C1A43CEC0000000290909090909090" },
	{ 0x041193, "6A01", "6A03" },
	{ 0x040F31, "8B8C24F80A000085C90F84E900000080395F0F84E0000000", "33D28B4424140FAFC550E8F06F160083C404EB7490909090" },
};

static const HookPatchItem g_colorIntensityPatch[] =
{
	{ 0x0418C5, "C7020000000033DBC70600000000", "E856661600E99800000090909090" },
};

static const HookPatchItem g_dat32bppPatch[] =
{
	{ 0x031A17, "EB3C", "EBB8" },
	{ 0x0319D1, "5800000000", "BB03000000" },
	{ 0x0319EB, "C744243C02000000", "C744243C00000000" },
	{ 0x031552, "744081E3FFFF00008D1C5BC1E303668B8B50B25F00", "33C06681FBA201740340EB0383C00489442410EB0B" },
	{ 0x031572, "8BC12500400000F7D81BC024FD83C006", "81E3FFFF00008D1C5BC1E303EB479090" },
	{ 0x0CD847, "E8A4000000", "E8D4A60D00" },
};

static const HookPatchItem g_datBcnPatch[] =
{
	{ 0x031AE3, "C744243400000000", "E838641700909090" },
	{ 0x195CA9, "8B5118", "8B5160" },
	{ 0x195CAC, "895588", "8955C0" },
};

static const HookPatchItem g_texturesLoadingPatch[] =
{
	{ 0x0CC45C, "33FF85C97E22", "E8BFBA0D0090" },
	{ 0x0CC47F, "473BF97CDE", "4F85FF7DDE" },
	{ 0x0CC562, "8B460833FF", "E8B9B90D00" },
	{ 0x0CC48E, "33FF85C97E26", "E88DBA0D0090" },
	{ 0x0CC4B5, "473BF97CDA", "4F85FF7DDA" },
	{ 0x0CC65E, "8B460833FF", "E8BDB80D00" },
};

static const HookPatch g_patches[] =
{
	MAKE_HOOK_PATCH("To call the hook that set the opt name", g_setOptNamePatch),
	MAKE_HOOK_PATCH("To call the hook that set textures bpp 8 to 32", g_setTexturesBpp8To32Patch),
	MAKE_HOOK_PATCH("To call the hook that set TextureAlphaMask", g_setTextureAlphaMaskPatch),
	MAKE_HOOK_PATCH("To call the hook that creates LightMap", g_createLightMapPatch),
	MAKE_HOOK_PATCH("To call the hook that set color intensity", g_colorIntensityPatch),
	MAKE_HOOK_PATCH("To call the hook that set dat bpp to 32", g_dat32bppPatch),
	MAKE_HOOK_PATCH("To call the hook that set dat bc7", g_datBcnPatch),
	MAKE_HOOK_PATCH("To call the hook that improves textures loading", g_texturesLoadingPatch),
};
