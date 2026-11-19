import {
  AfterViewInit, Component, ElementRef, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild
} from '@angular/core';
import * as L from 'leaflet';

/** A point on the map (WGS84 degrees) */
export interface MapPoint {
  latitude: number;
  longitude: number;
}

/**
 * OpenStreetMap map (Leaflet) showing the school location.
 * Click the map or drag the pin to pick the location (when not read-only).
 */
@Component({
  selector: 'app-school-location-map',
  standalone: false,
  template: `<div #map class="school-location-map"></div>`,
  styles: [`
    .school-location-map { height: 340px; width: 100%; border-radius: 12px; border: 1px solid #e6ecf5; z-index: 0; }
    :host ::ng-deep .school-pin {
      width: 30px; height: 30px; margin: -30px 0 0 -15px;
      border-radius: 50% 50% 50% 0; transform: rotate(-45deg);
      background: var(--bs-danger, #d92550); border: 3px solid #fff;
      box-shadow: 0 2px 6px rgba(0, 0, 0, .35);
    }
  `]
})
export class SchoolLocationMapComponent implements AfterViewInit, OnChanges, OnDestroy {
  /** Shown when the school has no location yet (Cairo) */
  static readonly defaultCenter: L.LatLngTuple = [30.0444, 31.2357];

  @Input() latitude: number | null = null;
  @Input() longitude: number | null = null;
  @Input() readOnly = false;

  /** The user clicked the map or dragged the pin */
  @Output() picked = new EventEmitter<MapPoint>();

  @ViewChild('map', { static: true }) private mapElement!: ElementRef<HTMLDivElement>;

  private map?: L.Map;
  private marker?: L.Marker;
  private resizeObserver?: ResizeObserver;

  // A CSS pin: Leaflet's default marker images are not bundled by the Angular build
  private readonly icon = L.divIcon({ className: '', html: '<div class="school-pin"></div>', iconSize: [0, 0] });

  ngAfterViewInit(): void {
    const point = this.point();
    this.map = L.map(this.mapElement.nativeElement, {
      center: point ?? SchoolLocationMapComponent.defaultCenter,
      zoom: point ? 16 : 11
    });
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(this.map);

    if (!this.readOnly) {
      this.map.on('click', (e: L.LeafletMouseEvent) => this.pick(e.latlng));
    }
    this.updateMarker(false);

    // The map is created inside a card that may still be laying out: fix the size when it changes
    this.resizeObserver = new ResizeObserver(() => this.map?.invalidateSize());
    this.resizeObserver.observe(this.mapElement.nativeElement);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if ((changes['latitude'] || changes['longitude']) && this.map) {
      this.updateMarker(true);
    }
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
    this.map?.remove();
  }

  private point(): L.LatLngTuple | null {
    const lat = this.latitude;
    const lng = this.longitude;
    const valid = lat !== null && lng !== null && Number.isFinite(lat) && Number.isFinite(lng)
      && Math.abs(lat) <= 90 && Math.abs(lng) <= 180;
    return valid ? [lat, lng] : null;
  }

  private pick(latlng: L.LatLng): void {
    // 6 decimals ≈ 10 cm: plenty for a school
    const round = (v: number) => Math.round(v * 1e6) / 1e6;
    this.picked.emit({ latitude: round(latlng.lat), longitude: round(latlng.wrap().lng) });
  }

  /** Put the pin on the current point (remove it when there is none) */
  private updateMarker(pan: boolean): void {
    const map = this.map!;
    const point = this.point();
    if (!point) {
      this.marker?.remove();
      this.marker = undefined;
      return;
    }
    if (this.marker) {
      this.marker.setLatLng(point);
    } else {
      this.marker = L.marker(point, { icon: this.icon, draggable: !this.readOnly }).addTo(map);
      this.marker.on('dragend', () => this.pick(this.marker!.getLatLng()));
    }
    if (pan && !map.getBounds().contains(point)) {
      map.setView(point, Math.max(map.getZoom(), 15));
    }
  }
}
