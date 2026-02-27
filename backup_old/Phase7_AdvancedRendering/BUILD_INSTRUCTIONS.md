# Build Instructions for Phase4 Graphics

## Critical Fix Applied - Native Renderer Update Required

### What Changed:
The native renderer (`NativeRenderer.dll`) has been modified to support proper text overlay rendering.

**Modified Files:**
- `NativeRenderer/renderer.cpp` - Added `SwapBuffersNow()` function
- `NativeRenderer/renderer.h` - Added `SwapBuffersNow()` declaration
- `WinFormsApp/CamViewerControl.cs` - Updated to call `SwapBuffersNow()` after text drawing

---

## Build Steps (Windows with Visual Studio)

### Option 1: Using CMake (Recommended)

1. **Open Developer Command Prompt for VS 2022** (or VS 2019)

2. **Navigate to NativeRenderer directory:**
   ```cmd
   cd Phase4_Graphics\NativeRenderer\build
   ```

3. **Generate build files:**
   ```cmd
   cmake ..
   ```

4. **Build the DLL:**
   ```cmd
   cmake --build . --config Release
   ```

5. **Copy DLL to output directory:**
   ```cmd
   copy Release\NativeRenderer.dll ..\..\WinFormsApp\bin\x64\Release\
   ```
   
   Or for Debug:
   ```cmd
   copy Release\NativeRenderer.dll ..\..\WinFormsApp\bin\x64\Debug\
   ```

---

### Option 2: Using Visual Studio Directly

1. **Open `Phase4_Graphics.sln` in Visual Studio**

2. **Set Platform to x64:**
   - Configuration Manager → Active solution platform → x64

3. **Build NativeRenderer project:**
   - Right-click `NativeRenderer` project → Build

4. **Build WinFormsApp project:**
   - Right-click `WinFormsApp` project → Build

5. **Run the application:**
   - Set `WinFormsApp` as startup project
   - Press F5 or click Run

---

## Verification

After building, verify the changes work:

### Test 1: MPF Geometry Rendering
- [ ] Load `1.MPF` file
- [ ] **Expected**: Geometry displays correctly (not blank black screen)
- [ ] **Expected**: Can see parts and contours

### Test 2: Part/Contour Number Toggle
- [ ] Click "파트 번호 (Part #)" button
- [ ] **Expected**: Part numbers appear WITHOUT hiding geometry
- [ ] Click "컨투어 번호 (Contour #)" button
- [ ] **Expected**: Contour numbers appear WITHOUT hiding geometry

### Test 3: Pan/Zoom with Numbers
- [ ] Enable part/contour numbers
- [ ] Pan with mouse
- [ ] **Expected**: Numbers stay visible and update smoothly
- [ ] Zoom with scroll wheel
- [ ] **Expected**: Numbers stay visible and scale correctly

### Test 4: Simulation
- [ ] Start simulation
- [ ] **Expected**: Reduced flickering compared to before
- [ ] **Expected**: Numbers remain visible during simulation

---

## Technical Details

### What the Fix Does:

**Before:**
```
OpenGL Render → SwapBuffers → GDI+ Text (gets erased)
```

**After:**
```
OpenGL Render → glFinish() → GDI+ Text → SwapBuffers → Display
```

### Key Changes:

1. **EndMPFRender()** now calls `glFinish()` instead of `SwapBuffers()`
   - Ensures all OpenGL commands complete
   - Does NOT present to screen yet

2. **SwapBuffersNow()** new function
   - Called after GDI+ text is drawn
   - Presents both OpenGL and GDI+ content together

3. **Paint Event Sequence:**
   ```csharp
   if (currentProgram != null)
   {
       // 1. Render OpenGL geometry
       RenderMPFScene(); // or RenderSimulationFrame()
       // EndMPFRender() is called inside, which does glFinish()
       
       // 2. Draw GDI+ text overlays
       DrawTextOverlays(e.Graphics);
       
       // 3. Present everything to screen
       NativeRenderer.SwapBuffersNow();
   }
   ```

---

## Troubleshooting

### DLL Not Found Error
**Solution**: Copy `NativeRenderer.dll` to the same directory as `WinFormsApp.exe`

### Still Seeing Blank Screen
**Possible causes:**
1. DLL not rebuilt - make sure to build `NativeRenderer` project
2. Wrong platform (x86 vs x64) - ensure both are x64
3. Old DLL cached - clean solution and rebuild all

### Text Still Disappears
**Possible causes:**
1. `SwapBuffersNow()` not being called - check that native renderer was rebuilt
2. P/Invoke signature mismatch - verify DLL exports match C# declarations

### Flickering Still Severe
**Note**: Some flickering is expected due to OpenGL + GDI+ mixing
**Improvements made:**
- Double buffering enabled
- 30ms throttle timer (~33 FPS)
- Optimized render sequence

---

## Alternative: Full Rebuild

If you encounter issues, try a full clean rebuild:

```cmd
# Clean all projects
msbuild Phase4_Graphics.sln /t:Clean /p:Configuration=Release /p:Platform=x64

# Rebuild all
msbuild Phase4_Graphics.sln /t:Rebuild /p:Configuration=Release /p:Platform=x64
```

---

## Files Modified (This Commit)

1. **NativeRenderer/renderer.cpp**
   - Modified `EndMPFRender()` to call `glFinish()` instead of `SwapBuffers()`
   - Added `SwapBuffersNow()` function

2. **NativeRenderer/renderer.h**
   - Added `SwapBuffersNow()` declaration

3. **WinFormsApp/CamViewerControl.cs**
   - Added `SwapBuffersNow()` P/Invoke declaration
   - Modified `RenderPanel_Paint()` to call `SwapBuffersNow()` after text drawing
   - Removed temporary `DrawTextOverlaysAfterSwap()` method

---

**After building, test with the sample file:**
`Phase4_Graphics/SampleMPF/upload_files/1.MPF`

Good luck! 🚀
