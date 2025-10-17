/**
 * HK MPF File Parser
 * HK 레이저 절단 프로그램 파일 파서
 */

import type {
  MPFProgram,
  Command,
  HKCommand,
  GCodeCommand,
  NBlockCommand,
  CommentCommand,
  HKLDBCommand,
  HKINICommand,
  HKOSTCommand,
  NestingInfo,
  Part,
  Contour,
  PathSegment,
  Point2D,
} from '../types';

export class MPFParser {
  private lines: string[] = [];
  private currentLine = 0;
  private commands: Command[] = [];

  /**
   * MPF 파일 파싱
   */
  parse(content: string): MPFProgram {
    this.lines = content.split('\n').map(line => line.trim()).filter(line => line.length > 0);
    this.currentLine = 0;
    this.commands = [];

    // 모든 라인을 커맨드로 파싱
    this.parseAllCommands();

    // 구조화된 데이터로 변환
    return this.buildMPFProgram();
  }

  /**
   * 모든 커맨드 파싱
   */
  private parseAllCommands(): void {
    for (let i = 0; i < this.lines.length; i++) {
      const line = this.lines[i];
      const command = this.parseLine(line);
      if (command) {
        this.commands.push(command);
      }
    }
  }

  /**
   * 한 줄 파싱
   */
  private parseLine(line: string): Command | null {
    // 주석 처리
    if (line.startsWith(';')) {
      return {
        type: 'COMMENT',
        text: line.substring(1).trim(),
      } as CommentCommand;
    }

    // N 블록 번호
    if (line.match(/^N\d+/)) {
      const match = line.match(/^N(\d+)/);
      if (match) {
        return {
          type: 'NBLOCK',
          blockNumber: parseInt(match[1]),
        } as NBlockCommand;
      }
    }

    // HK 함수
    if (line.startsWith('HK')) {
      return this.parseHKCommand(line);
    }

    // G-code
    if (line.match(/^[GM]\d+/) || line.match(/^[XYZ]/)) {
      return this.parseGCode(line);
    }

    return null;
  }

  /**
   * HK 함수 파싱
   */
  private parseHKCommand(line: string): HKCommand | null {
    const funcMatch = line.match(/^(HK[A-Z]+)/);
    if (!funcMatch) return null;

    const funcName = funcMatch[1];
    const argsMatch = line.match(/\(([^)]*)\)/);
    const args = argsMatch ? argsMatch[1].split(',').map(a => a.trim()) : [];

    switch (funcName) {
      case 'HKLDB':
        return {
          type: 'HKLDB',
          material: this.parseNumber(args[0]),
          dbName: args[1]?.replace(/"/g, '') || '',
          assistGas: this.parseNumber(args[2]),
        } as HKLDBCommand;

      case 'HKINI':
        return {
          type: 'HKINI',
          totalParts: this.parseNumber(args[0]),
          width: this.parseNumber(args[1]),
          height: this.parseNumber(args[2]),
        } as HKINICommand;

      case 'HKOST':
        return {
          type: 'HKOST',
          x: this.parseNumber(args[0]),
          y: this.parseNumber(args[1]),
          rotation: this.parseNumber(args[2]),
          partNumber: this.parseNumber(args[3]),
          contourCount: this.parseNumber(args[4]),
        } as HKOSTCommand;

      case 'HKPPP':
        return { type: 'HKPPP' };

      case 'HKSTR':
        return {
          type: 'HKSTR',
          piercingType: this.parseNumber(args[0]),
          cuttingType: this.parseNumber(args[1]),
          x: this.parseNumber(args[2]),
          y: this.parseNumber(args[3]),
          toolCompensation: this.parseNumber(args[4]),
          contourWidth: this.parseNumber(args[5]),
          contourHeight: this.parseNumber(args[6]),
        };

      case 'HKPIE':
        return { type: 'HKPIE' };

      case 'HKLEA':
        return {
          type: 'HKLEA',
          gCode: this.parseNumber(args[0]),
          x: this.parseNumber(args[1]),
          y: this.parseNumber(args[2]),
          i: this.parseNumber(args[3]),
          j: this.parseNumber(args[4]),
        };

      case 'HKCUT':
        return { type: 'HKCUT' };

      case 'HKSTO':
        return {
          type: 'HKSTO',
          gCode: this.parseNumber(args[0]),
          x: this.parseNumber(args[1]),
          y: this.parseNumber(args[2]),
          i: this.parseNumber(args[3]),
          j: this.parseNumber(args[4]),
          webOnOff: this.parseNumber(args[5]),
        };

      case 'HKPED':
        return { type: 'HKPED' };

      case 'HKEND':
        return { type: 'HKEND' };

      case 'HKSCRC':
        if (args.length >= 4) {
          return {
            type: 'HKSCRC',
            cuttingType: this.parseNumber(args[0]),
            cuttingKind: this.parseNumber(args[1]),
            x: this.parseNumber(args[2]),
            y: this.parseNumber(args[3]),
          };
        } else if (args.length === 1) {
          return {
            type: 'HKSCRC',
            params: this.parseNumber(args[0]),
          };
        }
        return { type: 'HKSCRC' };

      default:
        return null;
    }
  }

  /**
   * G-code 파싱
   */
  private parseGCode(line: string): GCodeCommand | null {
    const gcode: GCodeCommand = {
      type: 'GCODE',
      command: '',
    };

    // G 또는 M 코드 추출
    const cmdMatch = line.match(/^([GM]\d+)/);
    if (cmdMatch) {
      gcode.command = cmdMatch[1];
    }

    // X, Y, Z, I, J, F 파라미터 추출
    const xMatch = line.match(/X([-+]?[\d.]+)/);
    const yMatch = line.match(/Y([-+]?[\d.]+)/);
    const zMatch = line.match(/Z([-+]?[\d.]+)/);
    const iMatch = line.match(/I([-+]?[\d.]+)/);
    const jMatch = line.match(/J([-+]?[\d.]+)/);
    const fMatch = line.match(/F([-+]?[\d.]+)/);

    if (xMatch) gcode.x = parseFloat(xMatch[1]);
    if (yMatch) gcode.y = parseFloat(yMatch[1]);
    if (zMatch) gcode.z = parseFloat(zMatch[1]);
    if (iMatch) gcode.i = parseFloat(iMatch[1]);
    if (jMatch) gcode.j = parseFloat(jMatch[1]);
    if (fMatch) gcode.f = parseFloat(fMatch[1]);

    return gcode;
  }

  /**
   * MPF 프로그램 구조 생성
   */
  private buildMPFProgram(): MPFProgram {
    const version = this.extractVersion();
    const hkldb = this.extractHKLDB();
    const hkini = this.extractHKINI();
    const nesting = this.extractNesting();
    const parts = this.extractParts();

    return {
      version,
      hkldb,
      hkini,
      nesting,
      parts,
      workpiece: {
        width: hkini.width,
        height: hkini.height,
      },
      rawCommands: this.commands,
    };
  }

  /**
   * 버전 추출
   */
  private extractVersion(): string {
    const versionCmd = this.commands.find(
      cmd => cmd.type === 'COMMENT' && (cmd as CommentCommand).text.startsWith('!V')
    ) as CommentCommand;
    return versionCmd ? versionCmd.text : 'Unknown';
  }

  /**
   * HKLDB 추출
   */
  private extractHKLDB(): HKLDBCommand {
    const hkldb = this.commands.find(cmd => cmd.type === 'HKLDB') as HKLDBCommand;
    if (!hkldb) {
      throw new Error('HKLDB command not found');
    }
    return hkldb;
  }

  /**
   * HKINI 추출
   */
  private extractHKINI(): HKINICommand {
    const hkini = this.commands.find(cmd => cmd.type === 'HKINI') as HKINICommand;
    if (!hkini) {
      throw new Error('HKINI command not found');
    }
    return hkini;
  }

  /**
   * 네스팅 정보 추출
   */
  private extractNesting(): NestingInfo[] {
    const nesting: NestingInfo[] = [];
    let currentBlockNumber = 0;

    for (let i = 0; i < this.commands.length; i++) {
      const cmd = this.commands[i];

      // N 블록 번호 확인
      if (cmd.type === 'NBLOCK') {
        currentBlockNumber = (cmd as NBlockCommand).blockNumber;
      }

      // HKOST 발견
      if (cmd.type === 'HKOST') {
        const hkost = cmd as HKOSTCommand;
        nesting.push({
          partOriginBlockNumber: currentBlockNumber,
          origin: { x: hkost.x, y: hkost.y },
          rotation: hkost.rotation,
          partCodeBlockNumber: hkost.partNumber,
          contourCount: hkost.contourCount,
        });
      }

      // HKEND 발견 시 네스팅 정보 종료
      if (cmd.type === 'HKEND') {
        break;
      }
    }

    return nesting;
  }

  /**
   * 파트 정보 추출
   */
  private extractParts(): Part[] {
    const parts: Part[] = [];
    const nesting = this.extractNesting();

    for (const nest of nesting) {
      const part = this.extractPart(nest);
      if (part) {
        parts.push(part);
      }
    }

    return parts;
  }

  /**
   * 개별 파트 추출
   */
  private extractPart(nest: NestingInfo): Part | null {
    // 파트 시작 블록 찾기
    let startIdx = -1;
    for (let i = 0; i < this.commands.length; i++) {
      const cmd = this.commands[i];
      if (cmd.type === 'NBLOCK' && (cmd as NBlockCommand).blockNumber === nest.partCodeBlockNumber) {
        startIdx = i;
        break;
      }
    }

    if (startIdx === -1) return null;

    // 컨투어 추출
    const contours: Contour[] = [];
    let currentContour: Partial<Contour> | null = null;
    let currentBlockNumber = nest.partCodeBlockNumber;
    let currentPosition: Point2D = { x: 0, y: 0 };

    for (let i = startIdx; i < this.commands.length; i++) {
      const cmd = this.commands[i];

      // 블록 번호 업데이트
      if (cmd.type === 'NBLOCK') {
        currentBlockNumber = (cmd as NBlockCommand).blockNumber;
      }

      // HKSTR: 컨투어 시작
      if (cmd.type === 'HKSTR') {
        const hkstr = cmd as any;
        currentContour = {
          id: `contour-${currentBlockNumber}`,
          blockNumber: currentBlockNumber,
          piercingType: hkstr.piercingType,
          cuttingType: hkstr.cuttingType,
          piercingPosition: { x: hkstr.x, y: hkstr.y },
          toolCompensation: hkstr.toolCompensation,
          boundingBox: {
            width: hkstr.contourWidth,
            height: hkstr.contourHeight,
          },
          approachPath: [],
          cuttingPath: [],
          allSegments: [],
          endGCode: 0,
          endPosition: { x: 0, y: 0 },
        };
        currentPosition = { x: hkstr.x, y: hkstr.y };
      }

      // HKLEA: Lead-in 정보
      if (cmd.type === 'HKLEA' && currentContour) {
        const hklea = cmd as any;
        if (hklea.gCode > 0) {
          const segment = this.createSegment(
            hklea.gCode,
            currentPosition,
            { x: hklea.x, y: hklea.y },
            hklea.i,
            hklea.j
          );
          currentContour.leadIn = {
            gCode: hklea.gCode,
            path: segment ? [segment] : [],
          };
          currentPosition = { x: hklea.x, y: hklea.y };
        }
      }

      // G-code: 절단 경로
      if (cmd.type === 'GCODE' && currentContour) {
        const gcode = cmd as GCodeCommand;
        if (gcode.x !== undefined && gcode.y !== undefined) {
          const segment = this.createSegment(
            this.getGCodeNumber(gcode.command),
            currentPosition,
            { x: gcode.x, y: gcode.y },
            gcode.i || 0,
            gcode.j || 0
          );
          if (segment) {
            currentContour.cuttingPath!.push(segment);
          }
          currentPosition = { x: gcode.x, y: gcode.y };
        }
      }

      // HKSTO: 컨투어 종료
      if (cmd.type === 'HKSTO' && currentContour) {
        const hksto = cmd as any;
        currentContour.endGCode = hksto.gCode;
        currentContour.endPosition = { x: hksto.x, y: hksto.y };

        // 마지막 세그먼트 추가
        if (hksto.gCode > 0) {
          const segment = this.createSegment(
            hksto.gCode,
            currentPosition,
            { x: hksto.x, y: hksto.y },
            hksto.i,
            hksto.j
          );
          if (segment) {
            currentContour.cuttingPath!.push(segment);
          }
        }

        // 전체 세그먼트 통합
        currentContour.allSegments = [
          ...(currentContour.leadIn?.path || []),
          ...currentContour.approachPath!,
          ...currentContour.cuttingPath!,
        ];

        contours.push(currentContour as Contour);
        currentContour = null;
      }

      // HKPED: 파트 종료
      if (cmd.type === 'HKPED') {
        break;
      }
    }

    return {
      id: `part-${nest.partOriginBlockNumber}`,
      blockNumber: nest.partCodeBlockNumber,
      origin: nest.origin,
      rotation: nest.rotation,
      contours,
    };
  }

  /**
   * 경로 세그먼트 생성
   */
  private createSegment(
    gCode: number,
    start: Point2D,
    end: Point2D,
    i: number,
    j: number
  ): PathSegment | null {
    if (gCode === 1 || gCode === 0) {
      // G0, G1: 직선
      return {
        type: 'line',
        start,
        end,
      };
    } else if (gCode === 2 || gCode === 3) {
      // G2, G3: 원호
      const center = {
        x: start.x + i,
        y: start.y + j,
      };
      const radius = Math.sqrt(i * i + j * j);
      const startAngle = Math.atan2(start.y - center.y, start.x - center.x);
      const endAngle = Math.atan2(end.y - center.y, end.x - center.x);

      return {
        type: 'arc',
        start,
        end,
        center,
        radius,
        clockwise: gCode === 2,
        startAngle,
        endAngle,
      };
    }

    return null;
  }

  /**
   * G-code 문자열에서 숫자 추출
   */
  private getGCodeNumber(command: string): number {
    const match = command.match(/\d+/);
    return match ? parseInt(match[0]) : 0;
  }

  /**
   * 문자열을 숫자로 변환
   */
  private parseNumber(str: string | undefined): number {
    if (!str) return 0;
    const num = parseFloat(str);
    return isNaN(num) ? 0 : num;
  }
}

/**
 * MPF 파일 파싱 함수 (편의 함수)
 */
export function parseMPFFile(content: string): MPFProgram {
  const parser = new MPFParser();
  return parser.parse(content);
}
