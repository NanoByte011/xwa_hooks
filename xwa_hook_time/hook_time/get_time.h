#pragma once

int L0050E410Hook(int* params);

int L0050E430Hook(int* params);

int RenderHudTimeHook(int* params);
int HangarCheckInputsHook(int* params);

int AsteroidsAnimationHook(int* params);
int NullMobileObjectAnimationHook(int* params);
int ShipAnimationHook(int* params);
int ExplosionAnimationHook(int* params);

int TimeGetTimeHook(int* params);
int GetTickCountHook(int* params);

int UpdateAiFunctionHook(int* params);
