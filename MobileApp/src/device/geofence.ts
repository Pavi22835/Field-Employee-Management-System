// Client-side mirror of FEMS.Domain.Common.GeofenceCalculator — used only to show the
// employee their live distance to the assigned area before they tap Start Visit. The
// backend re-validates and enforces the geofence server-side (section 9); this is UX only.
export const GeofenceCalculator = {
  distanceMeters(lat1: number, lon1: number, lat2: number, lon2: number): number {
    const R = 6_371_000;
    const toRad = (deg: number) => (deg * Math.PI) / 180;
    const dLat = toRad(lat2 - lat1);
    const dLon = toRad(lon2 - lon1);
    const a =
      Math.sin(dLat / 2) ** 2 +
      Math.cos(toRad(lat1)) * Math.cos(toRad(lat2)) * Math.sin(dLon / 2) ** 2;
    return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  }
};
