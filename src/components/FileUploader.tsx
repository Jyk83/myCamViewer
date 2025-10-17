/**
 * File Uploader Component
 * MPF 파일 업로드 컴포넌트
 */

import { useRef } from 'react';
import type { ChangeEvent } from 'react';

interface FileUploaderProps {
  onFileLoad: (content: string, filename: string) => void;
}

export function FileUploader({ onFileLoad }: FileUploaderProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFileChange = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (e) => {
      const content = e.target?.result as string;
      onFileLoad(content, file.name);
    };
    reader.readAsText(file);
  };

  const handleClick = () => {
    fileInputRef.current?.click();
  };

  const handleSampleLoad = async (filename: string) => {
    try {
      const response = await fetch(`/samples/${filename}`);
      const content = await response.text();
      onFileLoad(content, filename);
    } catch (error) {
      console.error('샘플 파일 로딩 실패:', error);
      alert('샘플 파일을 불러올 수 없습니다.');
    }
  };

  return (
    <div style={{ padding: '20px', textAlign: 'center' }}>
      <input
        ref={fileInputRef}
        type="file"
        accept=".txt,.mpf,.nc"
        style={{ display: 'none' }}
        onChange={handleFileChange}
      />
      <div style={{ display: 'flex', gap: '10px', justifyContent: 'center', flexWrap: 'wrap' }}>
        <button
          onClick={handleClick}
          style={{
            padding: '12px 24px',
            fontSize: '16px',
            backgroundColor: '#2196f3',
            color: 'white',
            border: 'none',
            borderRadius: '4px',
            cursor: 'pointer',
            fontWeight: 'bold',
          }}
        >
          📁 MPF 파일 선택
        </button>
        <button
          onClick={() => handleSampleLoad('MS1T_A05.txt')}
          style={{
            padding: '12px 24px',
            fontSize: '16px',
            backgroundColor: '#4caf50',
            color: 'white',
            border: 'none',
            borderRadius: '4px',
            cursor: 'pointer',
            fontWeight: 'bold',
          }}
        >
          📄 샘플 1 (직선)
        </button>
        <button
          onClick={() => handleSampleLoad('MS20T_1.TXT')}
          style={{
            padding: '12px 24px',
            fontSize: '16px',
            backgroundColor: '#ff9800',
            color: 'white',
            border: 'none',
            borderRadius: '4px',
            cursor: 'pointer',
            fontWeight: 'bold',
          }}
        >
          📄 샘플 2 (원호)
        </button>
      </div>
      <p style={{ marginTop: '10px', color: '#666', fontSize: '14px' }}>
        HK 레이저 절단 프로그램 파일 (.txt, .mpf, .nc)
      </p>
    </div>
  );
}
