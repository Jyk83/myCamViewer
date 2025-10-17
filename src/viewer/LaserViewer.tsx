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
    scene.background = new THREE.Color(0xffffff);
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
    program.parts.forEach(part => {
      drawPart(scene, part, {
        showPiercing,
        showLeadIn,
        showApproach,
        showCutting,
        isSelected: part.id === selectedPartId,
      });
    });

    // 카메라 위치 조정
    fitCameraToWorkpiece(program.workpiece.width, program.workpiece.height);
  }, [program, selectedPartId, selectedContourId, showPiercing, showLeadIn, showApproach, showCutting]);

  /**
   * 워크피스 그리기
   */
  const drawWorkpiece = (scene: THREE.Scene, width: number, height: number) => {
    const geometry = new THREE.PlaneGeometry(width, height);
    const material = new THREE.MeshBasicMaterial({
      color: new THREE.Color(Colors.workpiece),
      side: THREE.DoubleSide,
    });
    const plane = new THREE.Mesh(geometry, material);
    plane.position.set(width / 2, height / 2, -1);
    scene.add(plane);

    // 테두리
    const edges = new THREE.EdgesGeometry(geometry);
    const line = new THREE.LineSegments(
      edges,
      new THREE.LineBasicMaterial({ color: new THREE.Color(Colors.workpieceBorder), linewidth: 2 })
    );
    line.position.set(width / 2, height / 2, -1);
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
      isSelected: boolean;
    }
  ) => {
    const group = new THREE.Group();
    group.position.set(part.origin.x, part.origin.y, 0);
    group.rotation.z = (part.rotation * Math.PI) / 180;

    // 원점 표시
    const originGeometry = new THREE.CircleGeometry(2, 16);
    const originMaterial = new THREE.MeshBasicMaterial({ color: new THREE.Color(Colors.partOrigin) });
    const originMarker = new THREE.Mesh(originGeometry, originMaterial);
    group.add(originMarker);

    // 컨투어 그리기
    part.contours.forEach(contour => {
      drawContour(group, contour, options);
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
    }
  ) => {
    // 피어싱 위치 표시
    if (options.showPiercing && contour.piercingType > 0) {
      const piercingGeometry = new THREE.CircleGeometry(1.5, 16);
      const piercingMaterial = new THREE.MeshBasicMaterial({ color: new THREE.Color(Colors.piercing) });
      const piercingMarker = new THREE.Mesh(piercingGeometry, piercingMaterial);
      piercingMarker.position.set(contour.piercingPosition.x, contour.piercingPosition.y, 0.1);
      group.add(piercingMarker);
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
