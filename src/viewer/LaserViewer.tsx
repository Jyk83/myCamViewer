/**
 * HK Laser Viewer - 3D/2D Visualization Component
 * Three.js 기반 레이저 절단 경로 시각화
 */

import { useEffect, useRef, useState } from 'react';
import * as THREE from 'three';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';
import type { MPFProgram, Part, Contour, PathSegment } from '../types';
import { Colors } from '../types';

interface LaserViewerProps {
  program: MPFProgram | null;
  selectedPartId?: string;
  selectedContourId?: string;
  showPiercing?: boolean;
  showLeadIn?: boolean;
  showApproach?: boolean;
  showCutting?: boolean;
  showPartLabels?: boolean;
  showContourLabels?: boolean;
  contourLabelSize?: number;
  viewMode?: '2D' | '3D';
}

export function LaserViewer({
  program,
  selectedPartId,
  selectedContourId,
  showPiercing = true,
  showLeadIn = true,
  showApproach = true,
  showCutting = true,
  showPartLabels = true,
  showContourLabels = true,
  contourLabelSize = 24,
  viewMode = '2D',
}: LaserViewerProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const sceneRef = useRef<THREE.Scene | null>(null);
  const cameraRef = useRef<THREE.OrthographicCamera | null>(null);
  const rendererRef = useRef<THREE.WebGLRenderer | null>(null);
  const controlsRef = useRef<OrbitControls | null>(null);

  // Three.js 초기화
  useEffect(() => {
    if (!containerRef.current) return;

    const container = containerRef.current;
    const width = container.clientWidth;
    const height = container.clientHeight;

    // Scene 생성
    const scene = new THREE.Scene();
    scene.background = new THREE.Color(Colors.workpiece); // 어두운 청록색 배경
    sceneRef.current = scene;

    // Camera 생성 (Orthographic for 2D view)
    const aspect = width / height;
    const frustumSize = 500;
    const camera = new THREE.OrthographicCamera(
      (frustumSize * aspect) / -2,
      (frustumSize * aspect) / 2,
      frustumSize / 2,
      frustumSize / -2,
      0.1,
      1000
    );
    camera.position.set(0, 0, 100);
    camera.lookAt(0, 0, 0);
    cameraRef.current = camera;

    // Renderer 생성
    const renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.setSize(width, height);
    renderer.setPixelRatio(window.devicePixelRatio);
    container.appendChild(renderer.domElement);
    rendererRef.current = renderer;

    // Controls 생성
    const controls = new OrbitControls(camera, renderer.domElement);
    controls.enableRotate = viewMode === '3D';
    controls.enablePan = true;
    controls.enableZoom = true;
    controls.mouseButtons = {
      LEFT: THREE.MOUSE.PAN,
      MIDDLE: THREE.MOUSE.DOLLY,
      RIGHT: THREE.MOUSE.ROTATE,
    };
    controlsRef.current = controls;

    // 조명 추가
    const ambientLight = new THREE.AmbientLight(0xffffff, 0.8);
    scene.add(ambientLight);

    const directionalLight = new THREE.DirectionalLight(0xffffff, 0.5);
    directionalLight.position.set(0, 0, 100);
    scene.add(directionalLight);

    // 애니메이션 루프
    const animate = () => {
      requestAnimationFrame(animate);
      controls.update();
      renderer.render(scene, camera);
    };
    animate();

    // 리사이즈 핸들러
    const handleResize = () => {
      const newWidth = container.clientWidth;
      const newHeight = container.clientHeight;
      const newAspect = newWidth / newHeight;

      camera.left = (frustumSize * newAspect) / -2;
      camera.right = (frustumSize * newAspect) / 2;
      camera.top = frustumSize / 2;
      camera.bottom = frustumSize / -2;
      camera.updateProjectionMatrix();

      renderer.setSize(newWidth, newHeight);
    };

    window.addEventListener('resize', handleResize);

    // Cleanup
    return () => {
      window.removeEventListener('resize', handleResize);
      container.removeChild(renderer.domElement);
      renderer.dispose();
    };
  }, [viewMode]);

  // 프로그램 시각화
  useEffect(() => {
    if (!program || !sceneRef.current) return;

    const scene = sceneRef.current;

    // 기존 오브젝트 제거 (조명 제외)
    const objectsToRemove = scene.children.filter(
      child => !(child instanceof THREE.Light)
    );
    objectsToRemove.forEach(obj => scene.remove(obj));

    // 워크피스 그리기
    drawWorkpiece(scene, program.workpiece.width, program.workpiece.height);

    // 그리드 그리기
    drawGrid(scene, program.workpiece.width, program.workpiece.height);

    // 파트 그리기
    program.parts.forEach((part, index) => {
      drawPart(scene, part, {
        showPiercing,
        showLeadIn,
        showApproach,
        showCutting,
        showPartLabels,
        showContourLabels,
        contourLabelSize,
        isSelected: part.id === selectedPartId,
        partIndex: index + 1, // 1부터 시작하는 파트 번호
      });
    });

    // 카메라 위치 조정
    fitCameraToWorkpiece(program.workpiece.width, program.workpiece.height);
  }, [program, selectedPartId, selectedContourId, showPiercing, showLeadIn, showApproach, showCutting, showPartLabels, showContourLabels, contourLabelSize]);

  /**
   * 컨투어 바운딩 박스 계산 (HKOST 원점 기준)
   * HKSTR 이동 이후 HKSTO까지의 모든 경로 좌표에서 min/max 계산
   */
  const calculateActualBoundingBox = (contour: Contour): { minX: number; minY: number; maxX: number; maxY: number } => {
    let minX = Infinity;
    let minY = Infinity;
    let maxX = -Infinity;
    let maxY = -Infinity;

    // 안전한 값 추가 헬퍼 함수
    const addPoint = (x: number | undefined, y: number | undefined) => {
      if (x !== undefined && y !== undefined && !isNaN(x) && !isNaN(y)) {
        minX = Math.min(minX, x);
        minY = Math.min(minY, y);
        maxX = Math.max(maxX, x);
        maxY = Math.max(maxY, y);
      }
    };

    // HKSTR 피어싱 위치부터 시작 (컨투어 로컬 좌표 0,0 기준)
    if (contour.piercingPosition) {
      addPoint(contour.piercingPosition.x, contour.piercingPosition.y);
    }

    // Lead-in 경로 (HKLEA)
    if (contour.leadIn && contour.leadIn.path) {
      contour.leadIn.path.forEach(segment => {
        if (segment.start) addPoint(segment.start.x, segment.start.y);
        if (segment.end) addPoint(segment.end.x, segment.end.y);
      });
    }

    // Approach 경로 (HKCUT 전)
    if (contour.approachPath) {
      contour.approachPath.forEach(segment => {
        if (segment.start) addPoint(segment.start.x, segment.start.y);
        if (segment.end) addPoint(segment.end.x, segment.end.y);
      });
    }

    // Cutting 경로 (HKCUT 이후 ~ HKSTO 전)
    if (contour.cuttingPath) {
      contour.cuttingPath.forEach(segment => {
        if (segment.start) addPoint(segment.start.x, segment.start.y);
        if (segment.end) addPoint(segment.end.x, segment.end.y);

        // 원호인 경우 정확한 바운딩 박스 계산
        if (segment.type === 'arc') {
          // 이미 파싱된 center와 radius 사용
          const centerX = segment.center.x;
          const centerY = segment.center.y;
          const radius = segment.radius;
          
          if (!isNaN(centerX) && !isNaN(centerY) && !isNaN(radius)) {
            // 원의 바운딩 박스 = 중심점 ± 반지름
            addPoint(centerX - radius, centerY - radius);
            addPoint(centerX + radius, centerY - radius);
            addPoint(centerX - radius, centerY + radius);
            addPoint(centerX + radius, centerY + radius);
          }
        }
      });
    }

    // HKSTO 종료 위치
    if (contour.endPosition) {
      addPoint(contour.endPosition.x, contour.endPosition.y);
    }

    // 바운딩 박스가 계산되지 않은 경우 HKSTR의 boundingBox 사용
    if (minX === Infinity || maxX === -Infinity || isNaN(minX) || isNaN(maxX)) {
      // HKSTR의 피어싱 위치를 (0, 0)으로 하고 boundingBox 크기 사용
      const width = contour.boundingBox?.width || 10;
      const height = contour.boundingBox?.height || 10;
      
      return {
        minX: 0,
        minY: 0,
        maxX: width,
        maxY: height,
      };
    }

    return { minX, minY, maxX, maxY };
  };

  /**
   * 워크피스 그리기
   */
  const drawWorkpiece = (scene: THREE.Scene, width: number, height: number) => {
    // 테두리만 점선으로 그리기 (배경은 씬 배경 사용)
    const points = [
      new THREE.Vector3(0, 0, -1),
      new THREE.Vector3(width, 0, -1),
      new THREE.Vector3(width, height, -1),
      new THREE.Vector3(0, height, -1),
      new THREE.Vector3(0, 0, -1),
    ];
    const geometry = new THREE.BufferGeometry().setFromPoints(points);
    const material = new THREE.LineDashedMaterial({
      color: new THREE.Color(Colors.workpieceBorder),
      linewidth: 1,
      dashSize: 5,
      gapSize: 3,
    });
    const line = new THREE.Line(geometry, material);
    line.computeLineDistances(); // 점선 렌더링에 필요
    scene.add(line);
  };

  /**
   * 그리드 그리기
   */
  const drawGrid = (scene: THREE.Scene, width: number, height: number) => {
    const gridHelper = new THREE.GridHelper(Math.max(width, height), 20, Colors.grid, Colors.grid);
    gridHelper.rotation.x = Math.PI / 2;
    gridHelper.position.set(width / 2, height / 2, -2);
    scene.add(gridHelper);
  };

  /**
   * 파트 그리기
   */
  const drawPart = (
    scene: THREE.Scene,
    part: Part,
    options: {
      showPiercing: boolean;
      showLeadIn: boolean;
      showApproach: boolean;
      showCutting: boolean;
      showPartLabels: boolean;
      showContourLabels: boolean;
      contourLabelSize: number;
      isSelected: boolean;
      partIndex: number;
    }
  ) => {
    const group = new THREE.Group();
    group.position.set(part.origin.x, part.origin.y, 0);
    group.rotation.z = (part.rotation * Math.PI) / 180;

    // 원점 표시는 하지 않음 (데이터는 group.position에 유지됨)
    // const originGeometry = new THREE.CircleGeometry(0.5, 16);
    // const originMaterial = new THREE.MeshBasicMaterial({ color: new THREE.Color(Colors.partOrigin) });
    // const originMarker = new THREE.Mesh(originGeometry, originMaterial);
    // originMarker.position.z = 0.6;
    // group.add(originMarker);

    // 파트 바운딩 박스 그리기 (마지막 컨투어의 바운딩 박스 사용)
    let partWidth = 0;
    let partHeight = 0;
    if (part.contours.length > 0) {
      const lastContour = part.contours[part.contours.length - 1];
      partWidth = lastContour.boundingBox.width;
      partHeight = lastContour.boundingBox.height;
      
      // 노란색 점선 박스
      const points = [
        new THREE.Vector3(0, 0, -0.5),
        new THREE.Vector3(partWidth, 0, -0.5),
        new THREE.Vector3(partWidth, partHeight, -0.5),
        new THREE.Vector3(0, partHeight, -0.5),
        new THREE.Vector3(0, 0, -0.5),
      ];
      const geometry = new THREE.BufferGeometry().setFromPoints(points);
      const material = new THREE.LineDashedMaterial({
        color: new THREE.Color(Colors.partLabel), // 노란색
        linewidth: 1,
        dashSize: 3,
        gapSize: 2,
      });
      const boundingBoxLine = new THREE.Line(geometry, material);
      boundingBoxLine.computeLineDistances(); // 점선 렌더링에 필요
      group.add(boundingBoxLine);
    }

    // 파트 번호 표시 (바운딩 박스 중앙) - 옵션이 활성화된 경우만
    if (options.showPartLabels) {
      const textSprite = createTextSprite(options.partIndex.toString(), Colors.partLabel, 24);
      textSprite.position.set(partWidth / 2, partHeight / 2, 0.7);
      // 스프라이트 크기는 월드 좌표로 설정 (줌에 따라 자동 조정됨)
      textSprite.scale.set(12, 6, 1);
      group.add(textSprite);
    }

    // 컨투어 그리기 (파트 내 인덱스 사용)
    part.contours.forEach((contour, index) => {
      drawContour(group, contour, options, index + 1);
    });

    scene.add(group);
  };

  /**
   * 컨투어 그리기
   */
  const drawContour = (
    group: THREE.Group,
    contour: Contour,
    options: {
      showPiercing: boolean;
      showLeadIn: boolean;
      showApproach: boolean;
      showCutting: boolean;
      showContourLabels: boolean;
      contourLabelSize: number;
    },
    contourIndex: number
  ) => {
    // 피어싱 위치 표시 (작은 빨간 점 - Points로 표시)
    if (options.showPiercing && contour.piercingType > 0) {
      const geometry = new THREE.BufferGeometry();
      const vertices = new Float32Array([
        contour.piercingPosition.x,
        contour.piercingPosition.y,
        0.5
      ]);
      geometry.setAttribute('position', new THREE.BufferAttribute(vertices, 3));
      const material = new THREE.PointsMaterial({
        color: new THREE.Color(Colors.piercing),
        size: 2,
        sizeAttenuation: false
      });
      const points = new THREE.Points(geometry, material);
      group.add(points);
    }

    // 바운딩 박스 계산 (항상 수행)
    const bbox = calculateActualBoundingBox(contour);
    const centerX = (bbox.minX + bbox.maxX) / 2;
    const topY = bbox.maxY;
    const bboxWidth = bbox.maxX - bbox.minX;
    const bboxHeight = bbox.maxY - bbox.minY;
    
    // 디버깅: 컨투어 번호 및 경로 정보 로그
    console.log(`컨투어 ${contourIndex}:`, {
      위치: `(${centerX.toFixed(2)}, ${topY.toFixed(2)})`,
      라벨위치Y: `${(topY + 2).toFixed(2)}`,
      크기: `${bboxWidth.toFixed(2)} x ${bboxHeight.toFixed(2)}`,
      bbox,
      piercingPos: contour.piercingPosition,
      endPos: contour.endPosition,
      leadInCount: contour.leadIn?.path?.length || 0,
      approachCount: contour.approachPath?.length || 0,
      cuttingCount: contour.cuttingPath?.length || 0,
    });
    
    // 컨투어 6번 상세 디버깅
    if (contourIndex === 6) {
      console.log('=== 컨투어 6번 상세 분석 ===');
      console.log('Cutting Path Segments:', contour.cuttingPath?.length || 0);
      contour.cuttingPath?.forEach((seg, idx) => {
        if (seg.type === 'arc') {
          console.log(`  Segment ${idx} (arc):`, {
            start: seg.start,
            end: seg.end,
            center: seg.center,
            radius: seg.radius,
            i: seg.i,
            j: seg.j,
            clockwise: seg.clockwise,
          });
        } else {
          console.log(`  Segment ${idx} (line):`, {
            start: seg.start,
            end: seg.end,
          });
        }
      });
    }
    
    // 컨투어 6번만 바운딩 박스 표시 (노란색 점선)
    if (contourIndex === 6) {
      const points = [
        new THREE.Vector3(bbox.minX, bbox.minY, 0.6),
        new THREE.Vector3(bbox.maxX, bbox.minY, 0.6),
        new THREE.Vector3(bbox.maxX, bbox.maxY, 0.6),
        new THREE.Vector3(bbox.minX, bbox.maxY, 0.6),
        new THREE.Vector3(bbox.minX, bbox.minY, 0.6),
      ];
      const geometry = new THREE.BufferGeometry().setFromPoints(points);
      const material = new THREE.LineDashedMaterial({
        color: new THREE.Color(Colors.partLabel), // 노란색
        linewidth: 2,
        dashSize: 3,
        gapSize: 2,
      });
      const boundingBoxLine = new THREE.Line(geometry, material);
      boundingBoxLine.computeLineDistances();
      group.add(boundingBoxLine);
      
      // 전역 변수에 컨투어 6번 정보 저장
      // @ts-ignore
      window.contour6Info = {
        width: bboxWidth,
        height: bboxHeight,
        bbox: bbox,
        centerX: centerX,
        topY: topY,
      };
    }
    
    // 컨투어 번호 표시 - 옵션이 활성화된 경우만
    if (options.showContourLabels) {
      // 유효한 좌표인 경우에만 라벨 생성
      if (!isNaN(centerX) && !isNaN(topY)) {
        const contourLabel = createTextSprite(contourIndex.toString(), Colors.contourLabel, options.contourLabelSize);
        // 컨투어 상단 중앙에 마진 2 추가
        contourLabel.position.set(centerX, topY + 2, 0.8);
        // 크기를 폰트 사이즈에 비례하여 조정
        const scale = options.contourLabelSize / 3;
        contourLabel.scale.set(scale, scale / 2, 1);
        group.add(contourLabel);
      } else {
        console.warn(`컨투어 ${contourIndex}: 유효하지 않은 좌표 - 라벨 생성 스킵`);
      }
    }

    // Lead-in 경로
    if (options.showLeadIn && contour.leadIn) {
      contour.leadIn.path.forEach(segment => {
        const line = createSegmentLine(segment, Colors.leadIn, 2);
        line.position.z = 0.2;
        group.add(line);
      });
    }

    // Approach 경로
    if (options.showApproach && contour.approachPath.length > 0) {
      contour.approachPath.forEach(segment => {
        const line = createSegmentLine(segment, Colors.approach, 2);
        line.position.z = 0.3;
        group.add(line);
      });
    }

    // Cutting 경로
    if (options.showCutting && contour.cuttingPath.length > 0) {
      const color = contour.cuttingType === 10 ? Colors.marking : Colors.cutting;
      contour.cuttingPath.forEach(segment => {
        const line = createSegmentLine(segment, color, 3);
        line.position.z = 0.4;
        group.add(line);
      });
    }
  };

  /**
   * 경로 세그먼트를 THREE.Line으로 변환
   */
  const createSegmentLine = (segment: PathSegment, color: string, linewidth: number): THREE.Line => {
    const points: THREE.Vector3[] = [];

    if (segment.type === 'line') {
      points.push(
        new THREE.Vector3(segment.start.x, segment.start.y, 0),
        new THREE.Vector3(segment.end.x, segment.end.y, 0)
      );
    } else if (segment.type === 'arc') {
      // 원호를 여러 개의 직선으로 근사
      const segments = 32;
      let startAngle = segment.startAngle;
      let endAngle = segment.endAngle;

      // 각도 정규화
      if (segment.clockwise) {
        if (endAngle > startAngle) {
          endAngle -= 2 * Math.PI;
        }
      } else {
        if (endAngle < startAngle) {
          endAngle += 2 * Math.PI;
        }
      }

      for (let i = 0; i <= segments; i++) {
        const t = i / segments;
        const angle = startAngle + (endAngle - startAngle) * t;
        const x = segment.center.x + segment.radius * Math.cos(angle);
        const y = segment.center.y + segment.radius * Math.sin(angle);
        points.push(new THREE.Vector3(x, y, 0));
      }
    }

    const geometry = new THREE.BufferGeometry().setFromPoints(points);
    const material = new THREE.LineBasicMaterial({ color: new THREE.Color(color), linewidth });
    return new THREE.Line(geometry, material);
  };

  /**
   * 텍스트 스프라이트 생성 (Canvas를 텍스처로 사용)
   */
  const createTextSprite = (text: string, color: string, fontSize: number): THREE.Sprite => {
    const canvas = document.createElement('canvas');
    const context = canvas.getContext('2d')!;
    // 더 높은 해상도로 선명하게 렌더링
    canvas.width = 256;
    canvas.height = 128;

    // 투명 배경
    context.clearRect(0, 0, canvas.width, canvas.height);

    // 텍스트 외곽선 (가독성 향상)
    context.strokeStyle = '#000000';
    context.lineWidth = 6;
    context.font = `bold ${fontSize * 6}px Arial`;
    context.textAlign = 'center';
    context.textBaseline = 'middle';
    context.strokeText(text, canvas.width / 2, canvas.height / 2);

    // 텍스트 그리기
    context.fillStyle = color;
    context.fillText(text, canvas.width / 2, canvas.height / 2);

    // 텍스처 생성
    const texture = new THREE.CanvasTexture(canvas);
    texture.needsUpdate = true;
    const material = new THREE.SpriteMaterial({ 
      map: texture, 
      transparent: true,
      depthTest: false,
      depthWrite: false
    });
    const sprite = new THREE.Sprite(material);
    sprite.scale.set(8, 4, 1);
    sprite.renderOrder = 999;

    return sprite;
  };

  /**
   * 카메라를 워크피스에 맞춤
   */
  const fitCameraToWorkpiece = (width: number, height: number) => {
    if (!cameraRef.current || !controlsRef.current) return;

    const camera = cameraRef.current;
    const controls = controlsRef.current;

    // 카메라 중심을 워크피스 중앙으로
    controls.target.set(width / 2, height / 2, 0);
    camera.position.set(width / 2, height / 2, 100);

    // 줌 조정
    const aspect = camera.right / camera.top;
    const frustumHeight = Math.max(height * 1.2, width * 1.2 / aspect);
    camera.top = frustumHeight / 2;
    camera.bottom = -frustumHeight / 2;
    camera.left = (-frustumHeight * aspect) / 2;
    camera.right = (frustumHeight * aspect) / 2;
    camera.updateProjectionMatrix();

    controls.update();
  };

  return (
    <div
      ref={containerRef}
      style={{
        width: '100%',
        height: '100%',
        position: 'relative',
        overflow: 'hidden',
      }}
    />
  );
}
