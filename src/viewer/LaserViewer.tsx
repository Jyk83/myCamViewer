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
        isSelected: part.id === selectedPartId,
        partIndex: index + 1, // 1부터 시작하는 파트 번호
      });
    });

    // 카메라 위치 조정
    fitCameraToWorkpiece(program.workpiece.width, program.workpiece.height);
  }, [program, selectedPartId, selectedContourId, showPiercing, showLeadIn, showApproach, showCutting, showPartLabels, showContourLabels]);

  /**
   * 실제 경로 좌표로 컨투어 바운딩 박스 계산
   * HKSTR의 contourWidth/Height가 아닌, 실제 그려진 경로의 좌표를 기반으로 계산
   */
  const calculateActualBoundingBox = (contour: Contour): { minX: number; minY: number; maxX: number; maxY: number } => {
    let minX = Infinity;
    let minY = Infinity;
    let maxX = -Infinity;
    let maxY = -Infinity;

    // 피어싱 위치 포함
    minX = Math.min(minX, contour.piercingPosition.x);
    minY = Math.min(minY, contour.piercingPosition.y);
    maxX = Math.max(maxX, contour.piercingPosition.x);
    maxY = Math.max(maxY, contour.piercingPosition.y);

    // Lead-in 경로 좌표 수집
    if (contour.leadIn) {
      contour.leadIn.path.forEach(segment => {
        minX = Math.min(minX, segment.start.x, segment.end.x);
        minY = Math.min(minY, segment.start.y, segment.end.y);
        maxX = Math.max(maxX, segment.start.x, segment.end.x);
        maxY = Math.max(maxY, segment.start.y, segment.end.y);
      });
    }

    // Approach 경로 좌표 수집
    contour.approachPath.forEach(segment => {
      minX = Math.min(minX, segment.start.x, segment.end.x);
      minY = Math.min(minY, segment.start.y, segment.end.y);
      maxX = Math.max(maxX, segment.start.x, segment.end.x);
      maxY = Math.max(maxY, segment.start.y, segment.end.y);
    });

    // Cutting 경로 좌표 수집
    contour.cuttingPath.forEach(segment => {
      minX = Math.min(minX, segment.start.x, segment.end.x);
      minY = Math.min(minY, segment.start.y, segment.end.y);
      maxX = Math.max(maxX, segment.start.x, segment.end.x);
      maxY = Math.max(maxY, segment.start.y, segment.end.y);

      // 원호인 경우 중심점과 반지름 고려
      if (segment.type === 'arc') {
        const centerX = segment.start.x + segment.i;
        const centerY = segment.start.y + segment.j;
        const radius = Math.sqrt(segment.i * segment.i + segment.j * segment.j);
        
        minX = Math.min(minX, centerX - radius);
        minY = Math.min(minY, centerY - radius);
        maxX = Math.max(maxX, centerX + radius);
        maxY = Math.max(maxY, centerY + radius);
      }
    });

    // 바운딩 박스가 없는 경우 (모든 좌표가 동일한 경우) 기본값 사용
    if (minX === Infinity || maxX === -Infinity) {
      return {
        minX: contour.piercingPosition.x,
        minY: contour.piercingPosition.y,
        maxX: contour.piercingPosition.x + contour.boundingBox.width,
        maxY: contour.piercingPosition.y + contour.boundingBox.height,
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

    // 파트 번호 표시 (바운딩 박스 하단 중앙) - 옵션이 활성화된 경우만
    if (options.showPartLabels) {
      const textSprite = createTextSprite(options.partIndex.toString(), Colors.partLabel, 28);
      // 바운딩 박스 하단 중앙에 배치 (기존 뷰어 레이아웃 참조)
      textSprite.position.set(partWidth / 2, -8, 1.0);
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

    // 컨투어 번호 표시 (실제 경로의 상단 중앙) - 옵션이 활성화된 경우만
    if (options.showContourLabels) {
      // 실제 경로 좌표로 바운딩 박스 계산
      const bbox = calculateActualBoundingBox(contour);
      const actualCenterX = (bbox.minX + bbox.maxX) / 2;
      const actualTopY = bbox.maxY;
      
      const contourLabel = createTextSprite(contourIndex.toString(), Colors.contourLabel, 16);
      // 컨투어 상단 중앙에 배치 (기존 뷰어 레이아웃 참조)
      contourLabel.position.set(actualCenterX, actualTopY + 3, 2.0);
      contourLabel.scale.set(4, 2, 1);
      group.add(contourLabel);
      
      // 디버깅: 컨투어 번호 로그
      console.log(`컨투어 ${contourIndex}: 위치 (${actualCenterX.toFixed(2)}, ${actualTopY.toFixed(2)}), bbox:`, bbox);
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
    // 더 높은 해상도로 설정하여 선명하게 렌더링
    canvas.width = 512;
    canvas.height = 256;

    // 배경 없음 (투명)
    context.clearRect(0, 0, canvas.width, canvas.height);

    // 텍스트 외곽선 (가독성 향상)
    context.strokeStyle = '#000000';
    context.lineWidth = 8;
    context.font = `bold ${fontSize * 8}px Arial`;
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
      depthTest: false, // 항상 위에 렌더링
      depthWrite: false
    });
    const sprite = new THREE.Sprite(material);
    sprite.scale.set(8, 4, 1); // 기본 크기 (호출하는 곳에서 조정 가능)
    sprite.renderOrder = 999; // 렌더링 순서를 가장 높게 설정

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
