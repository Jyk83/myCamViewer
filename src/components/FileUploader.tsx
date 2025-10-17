/**
 * File Uploader Component
 * MPF 파일 업로드 컴포넌트
 */

import { useRef, ChangeEvent } from 'react';

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

  return (
    <div style={{ padding: '20px', textAlign: 'center' }}>
      <input
        ref={fileInputRef}
        type="file"
        accept=".txt,.mpf,.nc"
        style={{ display: 'none' }}
        onChange={handleFileChange}
      />
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
      <p style={{ marginTop: '10px', color: '#666', fontSize: '14px' }}>
        HK 레이저 절단 프로그램 파일 (.txt, .mpf, .nc)
      </p>
    </div>
  );
}
